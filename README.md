# Wrath Compressor
![AppVeyor Status](https://ci.appveyor.com/api/projects/status/y7at8xd0ahes0g14)

Compress files into 7PK format for use with the Wrath2D engine.

## 7PK Specification

| Header |
|------------------------------|
| Magic number: 4 bytes: 7 P A K |
| Number of files: 4 bytes |
| Files are compressed?: 1 byte |

| File |
|------------------------------|
| File name length: 4 bytes |
| File name: x bytes |
| File content length: 4 bytes |
| File content: x bytes |
