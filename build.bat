docker buildx build -f Dockerfile --platform linux/arm64 --force-rm --no-cache -t epwatson/emailarchival ./
docker push epwatson/emailarchival
