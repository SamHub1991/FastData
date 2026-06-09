FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "./"]
COPY ["FastData.Untility/FastData.Untility.csproj", "FastData.Untility/"]
COPY ["FastData/FastData.csproj", "FastData/"]
COPY ["FastData.Demo/FastData.Demo.csproj", "FastData.Demo/"]
RUN dotnet restore "FastData.Demo/FastData.Demo.csproj"
COPY . .
RUN dotnet build "FastData.Demo/FastData.Demo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FastData.Demo/FastData.Demo.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "FastData.Demo.dll"]
