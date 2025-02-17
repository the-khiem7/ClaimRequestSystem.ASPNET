FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the test project and its dependencies
COPY ["ClaimRequest.UnitTest/ClaimRequest.UnitTest.csproj", "ClaimRequest.UnitTest/"]
COPY ["ClaimRequest.Service/ClaimRequest.BLL.csproj", "ClaimRequest.Service/"]
COPY ["ClaimRequest.Data/ClaimRequest.DAL.csproj", "ClaimRequest.Data/"]

# Restore the test project (which will pull in dependencies)
RUN dotnet restore "ClaimRequest.UnitTest/ClaimRequest.UnitTest.csproj"

# Copy the rest of the source code
COPY . .

# Create test results directory
RUN mkdir -p /TestResults

# Run tests
ENTRYPOINT ["dotnet", "test", "ClaimRequest.UnitTest/ClaimRequest.UnitTest.csproj", \
    "--logger:trx", \
    "--results-directory", "/TestResults", \
    "--verbosity", "detailed"] 