FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG TARGETARCH
WORKDIR /src
COPY --link . .
RUN dotnet restore -a $TARGETARCH

COPY --link . .
RUN dotnet test

WORKDIR /src/HomepageSC
RUN dotnet publish -a $TARGETARCH --no-restore -c Release -o /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime-deps:8.0-noble-chiseled-extra
WORKDIR /app

COPY --link --from=publish /app .
ENTRYPOINT ["./HomepageSC"]
