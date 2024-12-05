FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
ARG TARGETARCH
WORKDIR /src
COPY --link  HomepageSC/HomepageSC.csproj .
RUN dotnet restore -a $TARGETARCH

COPY --link . .
RUN dotnet test

RUN dotnet publish HomepageSC/HomepageSC.csproj -a $TARGETARCH --self-contained --no-restore -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy-chiseled-extra AS final
WORKDIR /app

COPY --link --from=publish /app .
ENTRYPOINT ["./HomepageSC"]
