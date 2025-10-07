# Verb: mf-hw

Print Media Foundation hardware codec support matrix as JSON.

Example:

```bash
captura-cli mf-hw
```

Sample output:

```json
{
  "GpuName": "NVIDIA GeForce RTX 3080",
  "Codecs": [
    {
      "Codec": "H264",
      "Encoder": { "HardwarePresent": true, "AcceptsNV12": true, "AcceptsP010": false },
      "Decoder": { "HardwarePresent": true, "OutputsNV12": true, "OutputsP010": false }
    },
    {
      "Codec": "HEVC",
      "Encoder": { "HardwarePresent": true, "AcceptsNV12": true, "AcceptsP010": true },
      "Decoder": { "HardwarePresent": true, "OutputsNV12": true, "OutputsP010": true }
    }
  ]
}
```
