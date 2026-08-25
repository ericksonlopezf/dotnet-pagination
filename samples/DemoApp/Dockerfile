FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy everything and restore
COPY ["samples/DemoApp/DemoApp.Api/DemoApp.Api.csproj", "samples/DemoApp/DemoApp.Api/"]
COPY ["samples/DemoApp/DemoApp.Application/DemoApp.Application.csproj", "samples/DemoApp/DemoApp.Application/"]
COPY ["samples/DemoApp/DemoApp.Domain/DemoApp.Domain.csproj", "samples/DemoApp/DemoApp.Domain/"]
COPY ["samples/DemoApp/DemoApp.Infrastructure/DemoApp.Infrastructure.csproj", "samples/DemoApp/DemoApp.Infrastructure/"]
COPY ["src/", "src/"]
RUN dotnet restore "samples/DemoApp/DemoApp.Api/DemoApp.Api.csproj"

# Build and publish
COPY . .
WORKDIR "/src/samples/DemoApp/DemoApp.Api"
RUN dotnet publish "DemoApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generate runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DemoApp.Api.dll"]
