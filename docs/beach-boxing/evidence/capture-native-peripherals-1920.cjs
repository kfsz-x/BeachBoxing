// Controlled replay of the supplied offline build. Does not modify the package.
const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');
const out = path.resolve(process.argv[2] || 'docs/beach-boxing/reference/native-peripherals-1920');
fs.mkdirSync(out, { recursive: true });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
let sequence = 0, socket;
const pending = new Map(), events = [], samples = [];
const chrome = spawn('C:/Program Files/Google/Chrome/Application/chrome.exe', [
  '--headless=new', '--enable-unsafe-swiftshader', '--disable-background-timer-throttling',
  '--disable-renderer-backgrounding', '--autoplay-policy=no-user-gesture-required',
  '--remote-debugging-port=28920', `--user-data-dir=${path.join(out, 'browser-profile')}`, 'about:blank'
], { windowsHide: true, stdio: 'ignore' });
const command = (method, params = {}) => new Promise((resolve, reject) => {
  const id = ++sequence;
  const timer = setTimeout(() => { pending.delete(id); reject(new Error(`CDP deadline: ${method}`)); }, 20000);
  pending.set(id, { resolve: result => { clearTimeout(timer); resolve(result); }, reject });
  socket.send(JSON.stringify({ id, method, params }));
});
const evaluate = async expression => {
  const result = await command('Runtime.evaluate', { expression, returnByValue: true });
  if (result.exceptionDetails) throw new Error(JSON.stringify(result.exceptionDetails));
  return result.result.value;
};
const key = (type, key, code, number) => command('Input.dispatchKeyEvent', { type, key, code, windowsVirtualKeyCode: number, nativeVirtualKeyCode: number });
const send = (object, method) => evaluate(`window.unityGame.SendMessage(${JSON.stringify(object)},${JSON.stringify(method)})`);
const screenshot = async (name, elapsed) => {
  const result = await command('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
  fs.writeFileSync(path.join(out, `${name}.png`), Buffer.from(result.data, 'base64'));
  samples.push({ name, elapsed, wallClock: new Date().toISOString() });
  console.log(name, elapsed);
};
(async () => {
  let pages;
  for (let i = 0; i < 60; i++) {
    try { pages = await (await fetch('http://127.0.0.1:28920/json/list')).json(); if (pages.length) break; } catch {}
    await sleep(250);
  }
  if (!pages?.length) throw new Error('Chrome CDP did not start');
  socket = new WebSocket(pages.find(page => page.type === 'page').webSocketDebuggerUrl);
  await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject; });
  socket.onmessage = message => {
    const data = JSON.parse(message.data);
    if (data.id) { const waiting = pending.get(data.id); pending.delete(data.id); if (data.error) waiting?.reject(data.error); else waiting?.resolve(data.result); }
    else if (data.method === 'Runtime.consoleAPICalled' || data.method === 'Runtime.exceptionThrown') events.push(data);
  };
  await command('Page.enable'); await command('Runtime.enable');
  await command('Emulation.setDeviceMetricsOverride', { width: 1920, height: 1080, deviceScaleFactor: 1, mobile: false });
  await command('Page.addScriptToEvaluateOnNewDocument', { source: `
    window.__nativePrograms=[];
    for(const Type of [window.WebGLRenderingContext,window.WebGL2RenderingContext]) {
      if(!Type) continue;
      const programs=new WeakSet(), p=Type.prototype, use=p.useProgram;
      let current;
      p.useProgram=function(program){current=program;return use.call(this,program)};
      for(const method of ['drawElements','drawArrays']) {
        const original=p[method];p[method]=function(...args){
          if(current&&!programs.has(current)) {
            programs.add(current);
            const shaders=this.getAttachedShaders(current).map(shader=>this.getShaderSource(shader));
            if(shaders.some(s=>s.includes('_Splat0')||s.includes('_GridScale'))) {
              const uniforms={};
              for(let i=0;i<this.getProgramParameter(current,this.ACTIVE_UNIFORMS);i++) {
                const name=this.getActiveUniform(current,i).name;
                if(!/unity_SH|SpecCube0_HDR|LightColor/.test(name))continue;
                const value=this.getUniform(current,this.getUniformLocation(current,name));
                uniforms[name]=ArrayBuffer.isView(value)?Array.from(value):value;
              }
              window.__nativePrograms.push({shaders,uniforms});
            }
          }
          return original.apply(this,args);
        };
      }
    }
  ` });
  await command('Page.navigate', { url: 'http://127.0.0.1:28919/index.html' });
  let ready = false;
  for (let i = 0; i < 180; i++) { ready = await evaluate('!!window.unityGame'); if (ready) break; await sleep(500); }
  if (!ready) throw new Error('Original Unity instance did not become ready');
  await sleep(9000);
  await screenshot('spawn',0);
  await evaluate('window.unityGame.SendMessage("SingleplayerPlayer","TryToOpenMenu",1)');
  await sleep(800); await screenshot('trainer',0);
  await send('TRAINER MENU','TurnOff'); await send('SingleplayerPlayer','ExitMenu');
  await sleep(400);
  await evaluate('window.unityGame.SendMessage("SingleplayerPlayer","TryToOpenMenu",2)');
  await sleep(800); await screenshot('daily',0);
  fs.writeFileSync(path.join(out,'viewport.json'),JSON.stringify(await evaluate('({width:innerWidth,height:innerHeight,dpr:devicePixelRatio,canvas:[...document.querySelectorAll("canvas")].map(c=>({width:c.width,height:c.height,rect:c.getBoundingClientRect().toJSON()}))})'),null,2));

})().catch(error => { console.error(error); process.exitCode=1; }).finally(async()=>{
  fs.writeFileSync(path.join(out,'capture.json'),JSON.stringify({samples,events},null,2));
  if(socket?.readyState===1){try{await command('Browser.close');}catch{}socket.close();}chrome.kill();
});
