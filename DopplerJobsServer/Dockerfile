#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["DopplerJobsServer/Doppler.Jobs.Server.csproj", "DopplerJobsServer/"]
COPY ["DopplerBillingJob/Doppler.Billing.Job.csproj", "DopplerBillingJob/"]
COPY ["Doppler.Database/Doppler.Database.csproj", "Doppler.Database/"]
COPY ["CrossCutting/CrossCutting.csproj", "CrossCutting/"]
COPY ["DopplerCurrencyJob/Doppler.Currency.Job.csproj", "DopplerCurrencyJob/"]
RUN dotnet restore "DopplerJobsServer/Doppler.Jobs.Server.csproj"
COPY . .
WORKDIR "/src/DopplerJobsServer"
RUN dotnet build "Doppler.Jobs.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Doppler.Jobs.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Doppler.Jobs.Server.dll"]