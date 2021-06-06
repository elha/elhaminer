# elhaminer
A Stratum Bitcoin/SHA256 miner wtitten in C#
based on 
- https://github.com/ma261065/DotNetStratumMiner
- https://github.com/lithander/Minimal-Bitcoin-Miner

Motivation: Wanted to understand the wokring principle behind stratum + mining. 

## status
Should fully work for Bitcoin/SHA256 mining. You'll never earn any money with this. 
It's just a minimal implementation of the whole thing.  

Can be usefull to check stratum servers.

## problems
- Submitted shares are rejected with "low difficulty". Would be nice if someone could detect the problem.

## usage
See output on appropriate parameters. Works cross-plattform as a simple .NET Core project.

## sample
```
elhaminer -o stratum.server.com:1234 -u username -p password
```
