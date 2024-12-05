FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG TARGETARCH
WORKDIR /src
COPY --link . .
RUN dotnet restore -a $TARGETARCH

COPY --link . .
RUN dotnet test

WORKDIR /src/HomepageSC
RUN dotnet publish -a $TARGETARCH --self-contained --no-restore -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled-extra
WORKDIR /app

COPY --link --from=publish /app .
ENTRYPOINT ["./HomepageSC"]
