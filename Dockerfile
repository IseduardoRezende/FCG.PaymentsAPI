FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["global.json", "./"]
COPY ["Contracts/", "Contracts/"]
COPY ["01- API/FCG.Payments.Api/FCG.Payments.Api.csproj", "01- API/FCG.Payments.Api/"]
COPY ["02- Core/FCG.Payments.Application/FCG.Payments.Application.csproj", "02- Core/FCG.Payments.Application/"]
COPY ["02- Core/FCG.Payments.Domain/FCG.Payments.Domain.csproj", "02- Core/FCG.Payments.Domain/"]
COPY ["03- Infrastructure/FCG.Payments.Infrastructure/FCG.Payments.Infrastructure.csproj", "03- Infrastructure/FCG.Payments.Infrastructure/"]
RUN dotnet restore "01- API/FCG.Payments.Api/FCG.Payments.Api.csproj"

COPY . .
RUN dotnet publish "01- API/FCG.Payments.Api/FCG.Payments.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "FCG.Payments.Api.dll"]
