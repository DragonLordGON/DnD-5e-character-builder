PROJECT := DndCharacterbuilder.csproj
CONFIG ?= Debug
TFM := net8.0
RUNTIME ?= linux-x64
PUBLISH_DIR := publish
PUBLISH_PROPS := -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

.PHONY: help restore build run clean publish publish-linux publish-win publish-all

help:
	@echo "Targets disponibles:"
	@echo "  make restore                 - Restaure les dépendances NuGet"
	@echo "  make build [CONFIG=Release]  - Compile le projet"
	@echo "  make run [CONFIG=Debug]      - Lance l'application"
	@echo "  make clean                   - Nettoie les artefacts de build"
	@echo "  make publish                 - Publie pour $(RUNTIME) en autonome (sans runtime .NET installé)"
	@echo "  make publish-linux           - Publie Linux autonome"
	@echo "  make publish-win             - Publie Windows autonome"
	@echo "  make publish-all             - Publie Linux + Windows autonomes"

restore:
	dotnet restore $(PROJECT)

build: restore
	dotnet build $(PROJECT) -c $(CONFIG)

run:
	dotnet run --project $(PROJECT) -c $(CONFIG)

clean:
	dotnet clean $(PROJECT)

publish: restore
	dotnet publish $(PROJECT) -c Release -f $(TFM) -r $(RUNTIME) --self-contained true $(PUBLISH_PROPS) -o $(PUBLISH_DIR)/$(RUNTIME)

publish-linux: restore
	dotnet publish $(PROJECT) -c Release -f $(TFM) -r linux-x64 --self-contained true $(PUBLISH_PROPS) -o $(PUBLISH_DIR)/linux-x64

publish-win: restore
	dotnet publish $(PROJECT) -c Release -f $(TFM) -r win-x64 --self-contained true $(PUBLISH_PROPS) -o $(PUBLISH_DIR)/win-x64

publish-all: publish-linux publish-win