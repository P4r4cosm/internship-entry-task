FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TicTacToe.API/TicTacToe.API.csproj", "TicTacToe.API/"]
COPY ["TicTacToe.Infrastructure/TicTacToe.Infrastructure.csproj", "TicTacToe.Infrastructure/"]
COPY ["TicTacToe.Application/TicTacToe.Application.csproj", "TicTacToe.Application/"]
COPY ["TicTacToe.Domain/TicTacToe.Domain.csproj", "TicTacToe.Domain/"]
RUN dotnet restore "TicTacToe.API/TicTacToe.API.csproj"
COPY . .
WORKDIR "/src/TicTacToe.API"
RUN dotnet build "./TicTacToe.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TicTacToe.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicTacToe.API.dll"]
