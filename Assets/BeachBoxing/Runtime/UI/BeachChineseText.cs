using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BeachBoxing
{
    /// <summary>
    /// Translates presentation strings without modifying saved names or game data.
    /// Unknown text is returned verbatim; replacements only match complete labels
    /// or the game's known, bounded display formats.
    /// </summary>
    public static class BeachChineseText
    {
        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.CultureInvariant);
        private static readonly Regex Level = new Regex(@"\ALV(?:L)?\s*(\d+)(?:\s+(Trainer))?\z", Options);
        private static readonly Regex Weight = new Regex(@"\A([+\-]?\s*\d+(?:\.\d+)?)\s*kg\z", Options);
        private static readonly Regex Day = new Regex(@"\ADAY\s*(\d+)\z", Options);
        private static readonly Regex TrainingSpeed = new Regex(@"\A([+\-]?\s*\d+(?:\.\d+)?%)\s+Training Speed\z", Options);
        private static readonly Regex StarAmount = new Regex(@"\A([+\-]?\s*\d+(?:\.\d+)?[kKmM]?)\s+Stars?\z", Options);
        private static readonly Regex TimeLeft = new Regex(@"\ATIME LEFT\s*:\s*(\d+(?::\d+)?)\z", Options);
        private static readonly Regex FighterValues = new Regex(@"\A(.+?)\s{2,}(\d+\s*/\s*\d+\s*/\s*\d+)\z", Options);
        private static readonly Regex NumericLabel = new Regex(@"\A[+\-xX×]?\s*\d+(?:\.\d+)?[kKmM%]?(?:\s*(?:->|→|/|:)\s*\d+(?:\.\d+)?[kKmM%]?)*\z", Options);

        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "(Press E)", "（按 E）" },
            { "Press E", "按 E" },
            { "Approach equipment or the boxing ring", "靠近器械或拳击擂台" },
            { "Keep working out to defeat stronger opponents!", "继续锻炼，击败更强的对手！" },
            { "BLOCK TO RECOVER STAMINA", "格挡恢复体力" },
            { "BLOCK to Recharge !", "格挡恢复体力！" },
            { "BLOCK to Recharge!", "格挡恢复体力！" },
            { "BLOCK", "格挡" },
            { "PUNCH", "出拳" },
            { "Change Name", "修改昵称" },
            { "CLAIMED", "已领取" },
            { "CLAIM", "领取" },
            { "Collect from Daily Rewards", "从每日奖励领取" },
            { "Connected to Server!", "已连接服务器！" },
            { "Disconnected from Server!", "与服务器断开连接！" },
            { "DAILY REWARDS", "每日奖励" },
            { "DEFEAT REWARDS", "击败奖励" },
            { "Space", "空格" },
            { "Escape", "Esc" },
            { "Defeat.", "战败。" },
            { "DEFEAT", "战败" },
            { "Enter ID...", "输入房间号…" },
            { "ENTER NAME...", "输入昵称…" },
            { "EQUIP", "装备" },
            { "UNEQUIP", "卸下" },
            { "EXIT", "退出" },
            { "FIGHT!", "开战！" },
            { "FIGHT", "开战" },
            { "Forward", "前进" },
            { "Backward", "后退" },
            { "Left", "向左" },
            { "Right", "向右" },
            { "Interact", "交互" },
            { "Join Room", "加入房间" },
            { "Jump", "跳跃" },
            { "KEYBINDS", "按键设置" },
            { "LEVEL UP", "升级" },
            { "LVL UP", "升级" },
            { "Locker", "储物柜" },
            { "Menu", "菜单" },
            { "MORE TRAINERS COMING!", "更多教练即将登场！" },
            { "Music Volume", "音乐音量" },
            { "SFX Volume", "音效音量" },
            { "Not Enough Funds!", "余额不足！" },
            { "OUT OF ENERGY", "体力耗尽" },
            { "PAUSED", "已暂停" },
            { "PERSONAL TRAINERS", "私人教练" },
            { "PICK OPPONENT", "选择对手" },
            { "Profanity Detected!", "昵称含有不当内容！" },
            { "Recommended:", "推荐：" },
            { "Recommended", "推荐" },
            { "RESUME", "继续游戏" },
            { "CONTINUE", "继续" },
            { "RETURN HOME", "返回主页" },
            { "Reward Collected!", "奖励已领取！" },
            { "Reward claimed", "奖励已领取" },
            { "REWARDS", "奖励" },
            { "Shop", "商店" },
            { "STATS", "属性" },
            { "TRAINEE", "学员" },
            { "TRY AGAIN", "再试一次" },
            { "VICTORY!", "胜利！" },
            { "VICTORY", "胜利" },
            { "WORKOUT FASTER", "加速锻炼" },
            { "You Defeated:", "你击败了：" },
            { "COMING SOON", "敬请期待" },
            { "YOU", "你" },
            { "Player", "玩家" },
            { "Opponent", "对手" },
            { "Chest", "胸部" },
            { "Arms", "手臂" },
            { "Back", "背部" },
            { "Legs", "腿部" },
            { "Health", "生命" },
            { "Stamina", "体力" },
            { "Guard", "防御" },
            { "Strength", "力量" },
            { "Coins", "金币" },
            { "Stars", "星星" },
            { "Training Speed", "训练速度" },
            { "Workout", "锻炼" },
            { "Trainer", "教练" },
            { "Rookie", "新秀" },
            { "Amateur", "业余拳手" },
            { "Amateur Champ", "业余冠军" },
            { "Semi Pro", "半职业拳手" },
            { "Rising Star", "明日之星" },
            { "Pro Boxer", "职业拳手" },
            { "Division Threat", "赛区强敌" },
            { "Title Challenger", "拳王挑战者" },
            { "Title Champion", "拳王" },
            { "World Champion", "世界冠军" },
            { "Boxing Legend", "拳坛传奇" },
            { "Rookie Gauntlet", "新秀挑战赛" },
            { "Amateur Gauntlet", "业余挑战赛" },
            { "Semi Pro Gauntlet", "半职业挑战赛" },
            { "Rising Star Gauntlet", "新星挑战赛" },
            { "Pro Boxer Gauntlet", "职业挑战赛" },
            { "Division Threat Gauntlet", "赛区强敌挑战赛" },
            { "Title Challenger Gauntlet", "拳王挑战赛" },
            { "Title Champion Gauntlet", "拳王争霸赛" },
            { "World Champion Gauntlet", "世界冠军挑战赛" },
            { "Boxing Legend Gauntlet", "拳坛传奇挑战赛" },

            // Original opponents (including every rematch) and the six trainers.
            { "Ramez", "拉梅兹" },
            { "Kyle", "凯尔" },
            { "Kenji", "健二" },
            { "The Mountain", "巨山" },
            { "Malik", "马利克" },
            { "Yuri", "尤里" },
            { "Ace", "王牌" },
            { "Minho", "敏浩" },
            { "Rico", "里科" },
            { "Kylian", "基利安" },
            { "Tyrese", "泰瑞斯" },
            { "Punkz", "庞克" },
            { "Don", "唐" },
            { "Takumi", "拓海" },
            { "Nikko", "尼科" },
            { "Darren", "达伦" },
            { "Amir", "阿米尔" },
            { "Alejandro", "亚历杭德罗" },
            { "Ramez (Rematch)", "拉梅兹（再战）" },
            { "HORSE", "战马" },
            { "Brad", "布拉德" },
            { "Dean", "迪恩" },
            { "Faheem", "法希姆" },
            { "Kyung", "景" },
            { "Robo", "罗博" },
            { "Kwame", "夸梅" },
            { "Koji", "浩二" },
            { "Kai", "凯" },
            { "HORSE (Rematch)", "战马（再战）" },
            { "Fang", "獠牙" },
            { "Rook", "鲁克" },
            { "Vex", "维克斯" },
            { "Zeno", "泽诺" },
            { "Tino", "蒂诺" },
            { "Lynx", "猞猁" },
            { "Zade", "扎德" },
            { "Ghost", "幽灵" },
            { "Striker", "突击手" },
            { "Tank", "坦克" },
            { "Buckshot", "霰弹" },
            { "Cain", "凯恩" },
            { "Snipe", "狙击手" },
            { "Malik (Rematch)", "马利克（再战）" },
            { "Kylian (Rematch)", "基利安（再战）" },
            { "Cody", "科迪" },
            { "Dion", "迪昂" },
            { "Nova", "诺娃" },
            { "Zara", "扎拉" },
            { "Serena", "塞蕾娜" },
            { "Yui", "结衣" },

            // Source NPC aliases are complete-name matches, never word replacements.
            { "beef", "牛肉" },
            { "ohyeahbeefribs", "哦耶牛肋排" },
            { "yewyuh", "耶呦" },
            { "yui <3", "结衣♥" },
            { "electrorayz", "电光射线" },
            { "camoldinho", "卡莫尔迪尼奥" },
            { "thexboxplum", "游戏盒梅子" },
            { "kwaky", "呱奇" },
            { "sloshie", "斯洛希" },
            { "sloshmaster", "水花大师" },
            { "ty shiii", "泰小希" },
            { "i luv it", "我喜欢" },
            { "aaaaaaaaaa", "啊啊啊啊啊" },
            { "Name name Nma ename", "名字名字又名字" },
            { "aewsadew", "艾萨迪" },
            { "thdthth", "特德特" },
            { "awdsdzsdczd", "阿德斯" },
            { "awd", "阿沃德" },
            { "yjgygjg", "尤吉格" },
            { "dsff", "迪斯夫" },
            { "dream", "梦想" },
            { "mr beast", "野兽先生" },
            { "what the he", "怎么回事" },
            { "steakcore", "牛排核心" },
            { "zaptar", "扎普塔" },
            { "wagz", "瓦格兹" },
            { "toesnax", "脚趾小食" },
            { "flimsyx", "小脆皮" },
            { "ratloop", "鼠圈" },
            { "skvshy", "软绵绵" },
            { "sl4mjuice", "重击果汁" },
            { "horsington", "霍辛顿" },
            { "grogrogrogro", "咕噜咕噜" },
            { "grok learning", "格罗克学习中" },
            { "bunjipunch", "兔兔重拳" },
            { "schnorp", "施诺普" },
            { "lobstaah", "大龙虾" },
            { "zinki", "津奇" },
            { "Scooby DOoby", "史酷比杜比" },
            { "booyah", "好耶" },
            { "meow", "喵" },
            { "woof", "汪" },
            { "ROOOOAAARRRRRR", "嗷呜" },
            { "idk", "我不知道" },
            { "name", "名字" },
            { "loiksddhjk b", "洛伊克斯" },
            { "trh", "特尔赫" },
            { "nice man", "好好先生" },
            { "ok", "好的" },
            { "lmfao", "笑翻了" },
            { "lol", "哈哈" },
            { "gross", "好恶心" },
            { "idc dude", "随便啦老兄" },
            { "idc", "无所谓" },
            { "bonny", "邦妮" },
            { "bunny", "小兔" },
            { "bounce .", "弹弹。" },
            { "mouse ndjakwd", "老鼠小杰" },
            { "shush", "嘘" },
            { "pppppppppppppppppoouuink", "噗噗小猪" }
        };

        private static readonly Dictionary<string, string> Adjectives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Angry", "生气的" },
            { "Cool", "酷酷的" },
            { "Messy", "邋遢的" },
            { "Happy", "快乐的" },
            { "Stinky", "臭臭的" },
            { "Smelly", "臭烘烘的" },
            { "Old", "年老的" },
            { "Pretty", "漂亮的" },
            { "Hungry", "饥饿的" },
            { "Epic", "传奇的" },
            { "Stealthy", "隐秘的" },
            { "Cheesy", "奶酪味的" },
            { "Sluggish", "慢吞吞的" },
            { "Silly", "傻乎乎的" },
            { "Tiny", "小小的" },
            { "Huge", "巨大的" },
            { "Buzzing", "嗡嗡叫的" },
            { "Stunning", "惊艳的" },
            { "Mighty", "威猛的" },
            { "Strong", "强壮的" },
            { "Loose", "松垮的" },
            { "Bold", "大胆的" },
            { "Cursed", "被诅咒的" },
            { "Cringe", "尴尬的" },
            { "Flashy", "炫酷的" },
            { "Hasty", "急躁的" },
            { "Witty", "机智的" },
            { "Icy", "冰冷的" },
            { "Fresh", "清新的" },
            { "Sad", "伤心的" },
            { "Furious", "狂怒的" },
            { "Cozy", "惬意的" },
            { "Warm", "温暖的" },
            { "Cute", "可爱的" },
            { "Good", "乖巧的" },
            { "Loving", "有爱的" },
            { "Scared", "胆小的" },
            { "Yummy", "美味的" },
            { "Tasty", "可口的" },
            { "Fried", "油炸的" },
            { "Young", "年轻的" },
            { "Great", "了不起的" },
            { "Fierce", "凶猛的" },
            { "Brave", "勇敢的" }
        };

        private static readonly Dictionary<string, string> Nouns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Gorilla", "大猩猩" },
            { "Goose", "鹅" },
            { "Monkey", "猴子" },
            { "Chimp", "黑猩猩" },
            { "Pigeon", "鸽子" },
            { "Gecko", "壁虎" },
            { "Pig", "小猪" },
            { "Bunny", "兔子" },
            { "Cat", "猫" },
            { "Dog", "狗" },
            { "Beast", "野兽" },
            { "Monster", "怪兽" },
            { "Poop", "便便" },
            { "Lion", "狮子" },
            { "Wolf", "狼" },
            { "Apple", "苹果" },
            { "Bread", "面包" },
            { "Banana", "香蕉" },
            { "Egg", "鸡蛋" },
            { "Fish", "鱼" },
            { "Foot", "脚丫" },
            { "Bird", "小鸟" },
            { "Snake", "蛇" },
            { "Bear", "熊" },
            { "Beef", "牛肉" },
            { "Pen", "钢笔" },
            { "Ape", "猿猴" },
            { "Gun", "枪" },
            { "Sword", "剑" },
            { "Face", "脸蛋" },
            { "Crow", "乌鸦" },
            { "Sock", "袜子" },
            { "Rock", "石头" },
            { "Pebble", "鹅卵石" },
            { "Hat", "帽子" },
            { "Nose", "鼻子" },
            { "Head", "脑袋" },
            { "Tree", "大树" },
            { "Boat", "小船" },
            { "Pants", "裤子" },
            { "Turtle", "乌龟" },
            { "Undies", "内裤" },
            { "Mouse", "老鼠" },
            { "Car", "汽车" }
        };

        public static string Translate(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            string normalized = value.Replace("\r\n", "\n").Replace("\\n", "\n").Trim();
            if (normalized.Length == 0) return value;

            string translated;
            if (TryTranslateLabel(normalized, out translated)) return translated;

            // A result may contain an opponent name followed by the reward status.
            // Requiring every line to be known avoids altering arbitrary user prose.
            if (normalized.IndexOf('\n') >= 0)
            {
                string[] lines = normalized.Split('\n');
                bool changed = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    if (TryTranslateLabel(line, out translated))
                    {
                        lines[i] = translated;
                        changed = true;
                    }
                    else if (!NumericLabel.IsMatch(line))
                    {
                        return value;
                    }
                }
                if (changed) return string.Join("\n", lines);
            }

            return value;
        }

        private static bool TryTranslateLabel(string value, out string translated)
        {
            if (Labels.TryGetValue(value, out translated)) return true;

            string compact = Whitespace.Replace(value, " ");
            if (Labels.TryGetValue(compact, out translated))
            {
                // Preserve deliberate two-line button captions from the scene.
                if (value.IndexOf('\n') >= 0)
                {
                    if (compact.Equals("RETURN HOME", StringComparison.OrdinalIgnoreCase)) translated = "返回\n主页";
                    else if (compact.Equals("TRY AGAIN", StringComparison.OrdinalIgnoreCase)) translated = "再试\n一次";
                    else if (compact.Equals("WORKOUT FASTER", StringComparison.OrdinalIgnoreCase)) translated = "加速\n锻炼";
                    else if (compact.Equals("Rookie Gauntlet", StringComparison.OrdinalIgnoreCase)) translated = "新秀\n挑战赛";
                    else if (compact.Equals("Collect from Daily Rewards", StringComparison.OrdinalIgnoreCase)) translated = "从每日奖励\n领取";
                }
                return true;
            }

            if (TryTranslateGeneratedName(compact, out translated)) return true;

            Match match = Level.Match(compact);
            if (match.Success)
            {
                translated = match.Groups[2].Success ? match.Groups[1].Value + "级教练" : "等级 " + match.Groups[1].Value;
                return true;
            }
            match = Weight.Match(compact);
            if (match.Success)
            {
                translated = match.Groups[1].Value + "公斤";
                return true;
            }
            match = Day.Match(compact);
            if (match.Success)
            {
                translated = "第" + match.Groups[1].Value + "天";
                return true;
            }
            match = TrainingSpeed.Match(compact);
            if (match.Success)
            {
                translated = match.Groups[1].Value + " 训练速度";
                return true;
            }
            match = StarAmount.Match(compact);
            if (match.Success)
            {
                translated = match.Groups[1].Value + " 星星";
                return true;
            }
            match = TimeLeft.Match(compact);
            if (match.Success)
            {
                translated = "剩余时间：" + match.Groups[1].Value;
                return true;
            }

            match = FighterValues.Match(value);
            if (match.Success && TryTranslateIdentity(match.Groups[1].Value, out translated))
            {
                translated += "  " + match.Groups[2].Value;
                return true;
            }

            translated = null;
            return false;
        }

        internal static bool TryTranslateGeneratedName(string value, out string translated)
        {
            int space = value.IndexOf(' ');
            if (space > 0 && Adjectives.TryGetValue(value.Substring(0, space), out var adjective) &&
                Nouns.TryGetValue(value.Substring(space + 1), out var noun))
            {
                translated = adjective + noun;
                return true;
            }

            translated = null;
            return false;
        }

        private static bool TryTranslateIdentity(string value, out string translated)
        {
            // This template is emitted by BeachHud.Values; only known identities qualify.
            return Labels.TryGetValue(value.Trim(), out translated);
        }
    }
}
