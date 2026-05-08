run:
	dotnet run --project api

build:
	dotnet build api

test:
	dotnet test

format:
	dotnet csharpier format .

migration:
	@if [ -z "$(name)" ]; then \
		echo "❌ Missing migration name. Usage: make migration name=AddNewTable"; \
		exit 1; \
	fi; \
	ASPNETCORE_ENVIRONMENT=Development dotnet ef migrations add $(name) --project api
