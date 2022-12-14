#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Buffalo.Sample/Buffalo.Sample.csproj", "Buffalo.Sample/"]
COPY ["TTI.Buffalo.AmazonS3/TTI.Buffalo.AmazonS3.csproj", "TTI.Buffalo.AmazonS3/"]
COPY ["TTI.Buffalo/TTI.Buffalo.csproj", "TTI.Buffalo/"]
COPY ["TTI.Buffalo.GoogleCloud/TTI.Buffalo.GoogleCloud.csproj", "TTI.Buffalo.GoogleCloud/"]
RUN dotnet restore "Buffalo.Sample/Buffalo.Sample.csproj"
COPY . .
WORKDIR "/src/Buffalo.Sample"
RUN dotnet build "Buffalo.Sample.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Buffalo.Sample.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Buffalo.Sample.dll"]