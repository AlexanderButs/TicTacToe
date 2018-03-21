FROM microsoft/aspnetcore:2.0.6 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0.6-2.1.101 as build
WORKDIR /src
COPY . .
RUN dotnet restore
WORKDIR /src/src/TicTacToe
RUN dotnet build --no-restore -c Release -o /app

FROM build AS publish
RUN dotnet publish --no-restore -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT [ "dotnet", "TicTacToe.dll" ]