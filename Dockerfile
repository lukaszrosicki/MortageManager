FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MortgagePro.WebUI/MortgagePro.WebUI.csproj", "MortgagePro.WebUI/"]
COPY ["MortgagePro.Application/MortgagePro.Application.csproj", "MortgagePro.Application/"]
COPY ["MortgagePro.Domain/MortgagePro.Domain.csproj", "MortgagePro.Domain/"]
COPY ["MortgagePro.Infrastructure/MortgagePro.Infrastructure.csproj", "MortgagePro.Infrastructure/"]
RUN dotnet restore "MortgagePro.WebUI/MortgagePro.WebUI.csproj"

COPY . .
WORKDIR "/src/MortgagePro.WebUI"
RUN dotnet build "MortgagePro.WebUI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MortgagePro.WebUI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "MortgagePro.WebUI.dll"]
