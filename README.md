<img src="assets/NSS-128x128.png" align="right" />

# NuGetCachingProxy

[![Docker Image CI](https://github.com/nefarius/NuGetCachingProxy/actions/workflows/docker-image.yml/badge.svg)](https://github.com/nefarius/NuGetCachingProxy/actions/workflows/docker-image.yml) ![Docker Pulls](https://img.shields.io/docker/pulls/containinger/nugetcachingproxy)

Self-hosted reverse caching proxy for [`api.nuget.org`](https://api.nuget.org/) (official [NuGet.org](https://www.nuget.org/) backend).

<details><summary>Docker build</summary>

```PowerShell
docker build -t containinger/nugetcachingproxy:latest . ; docker push containinger/nugetcachingproxy:latest
```

</details>

## Sources & 3rd party credits

This application benefits from these awesome projects ❤ (appearance in no special order):

- [Microsoft YARP](https://microsoft.github.io/reverse-proxy/)
- [FastEndpoints](https://fast-endpoints.com/)
- [MongoDB Entities](https://mongodb-entities.com/)
