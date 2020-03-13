FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Doppler.Jobs.Server/Doppler.Service.Job.Server.csproj", "Doppler.Jobs.Server/"]
COPY ["CrossCutting/CrossCutting.csproj", "CrossCutting/"]
COPY ["Doppler.Sap.Job/Doppler.Sap.Job.Service.csproj", "Doppler.Sap.Job/"]
RUN dotnet restore "Doppler.Jobs.Server/Doppler.Service.Job.Server.csproj"
COPY . .
WORKDIR "/src/Doppler.Jobs.Server"
RUN dotnet build "Doppler.Service.Job.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Doppler.Service.Job.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Doppler.Service.Job.Server.dll"]