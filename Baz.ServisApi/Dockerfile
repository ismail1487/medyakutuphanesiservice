#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["bazmedyakutuphanesi/Baz.ServisApi/Baz.MedyaServiceApi.csproj", "bazmedyakutuphanesi/Baz.ServisApi/"]
COPY ["bazmedyakutuphanesi/Baz.Service/Baz.Service.csproj", "bazmedyakutuphanesi/Baz.Service/"]
COPY packages /src/packages
RUN dotnet restore "bazmedyakutuphanesi/Baz.ServisApi/Baz.MedyaServiceApi.csproj" --source /src/packages  --source https://api.nuget.org/v3/index.json
COPY bazmedyakutuphanesi/Baz.ServisApi bazmedyakutuphanesi/Baz.ServisApi
COPY bazmedyakutuphanesi/Baz.Service bazmedyakutuphanesi/Baz.Service
WORKDIR "/src/bazmedyakutuphanesi/Baz.ServisApi"
RUN dotnet build "Baz.MedyaServiceApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Baz.MedyaServiceApi.csproj" -c Release -o /app/publish

FROM base AS final

# Change timezone to local time
ENV TZ=Europe/Istanbul
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Baz.MedyaApi.dll"]
