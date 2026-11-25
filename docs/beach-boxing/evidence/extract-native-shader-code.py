"""Extract the shipped GLSL that UnityPy's ShaderLab exporter currently omits.

Usage: python extract-native-shader-code.py data.unity3d "TextMeshPro/Distance Field" output_dir
Requires UnityPy. Reads source only; output is evidence outside Assets.
"""
import argparse
import pathlib
import re

import UnityPy
from UnityPy.helpers import CompressionHelper

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("source", type=pathlib.Path)
parser.add_argument("shader_name")
parser.add_argument("output", type=pathlib.Path)
args = parser.parse_args()
source = args.source / "data.unity3d" if args.source.is_dir() else args.source
args.output.mkdir(parents=True, exist_ok=True)
written = 0
for obj in UnityPy.load(str(source)).objects:
    if obj.type.name != "Shader":
        continue
    shader = obj.read()
    if args.shader_name not in shader.m_ParsedForm.m_Name:
        continue
    name = shader.m_ParsedForm.m_Name.replace("/", "-")
    for platform in range(len(shader.offsets)):
        for segment, (offset, length, size) in enumerate(zip(
            shader.offsets[platform], shader.compressedLengths[platform], shader.decompressedLengths[platform]
        )):
            data = CompressionHelper.decompress_lz4(bytes(shader.compressedBlob[offset:offset + length]), size)
            for index, code in enumerate(re.findall(rb"[\x09\x0a\x0d\x20-\x7e]{80,}", data)):
                target = args.output / f"{name}-platform{platform}-segment{segment}-code{index}.txt"
                target.write_bytes(code)
                print(f"{target}: {len(code)} bytes (source Shader pathId {obj.path_id})")
                written += 1
if not written:
    raise SystemExit("No matching shader source strings were found.")
