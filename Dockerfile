FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["FCG.UsersAPI.Api/FCG.UsersAPI.Api.csproj", "FCG.UsersAPI.Api/"]
COPY ["FCG.UsersAPI.Application/FCG.UsersAPI.Application.csproj", "FCG.UsersAPI.Application/"]
COPY ["FCG.UsersAPI.Domain/FCG.UsersAPI.Domain.csproj", "FCG.UsersAPI.Domain/"]
COPY ["FCG.UsersAPI.Infra/FCG.UsersAPI.Infra.csproj", "FCG.UsersAPI.Infra/"]

RUN dotnet restore "FCG.UsersAPI.Api/FCG.UsersAPI.Api.csproj"

COPY . .

RUN dotnet publish "FCG.UsersAPI.Api/FCG.UsersAPI.Api.csproj" \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FCG.UsersAPI.Api.dll"]