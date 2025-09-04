.PHONY: help conf clean install-melonloader setup uninstall build fix-permissions dev watch shell logs

# Default target
help:
	@echo "Tower Dominion Mod Loader - Available Commands:"
	@echo ""
	@echo "Setup Commands:"
	@echo "  make setup              - Complete setup (config + MelonLoader if needed)"
	@echo "  make conf               - Create Directory.Build.props configuration"
	@echo "  make install-melonloader - Download and install MelonLoader"
	@echo "  make fix-permissions    - Make all scripts executable"
	@echo ""
	@echo "Development Commands:"
	@echo "  make build              - Build the mod (requires game to be run once)"
	@echo "  make dev                - Create symlink from game/Mods to build output"
	@echo "  make watch              - Watch for changes and auto-rebuild"
	@echo "  make clean              - Clean build artifacts"
	@echo "  make shell              - Open development shell in game directory"
	@echo "  make logs               - Follow MelonLoader log output in real-time"
	@echo ""
	@echo "Maintenance Commands:"
	@echo "  make uninstall          - Remove MelonLoader from game directory"
	@echo "  make help               - Show this help message"
	@echo ""
	@echo "Getting Started:"
	@echo "  1. Run 'make setup' to configure and install MelonLoader"
	@echo "  2. Launch Tower Dominion once to generate Il2Cpp assemblies"
	@echo "  3. Run 'make build' to build your mod"
	@echo "  4. Run 'make dev' to link build output to game Mods folder"
	@echo "  5. Run 'make watch' for continuous development with auto-rebuild"

# Function to check if MelonLoader is installed
# Usage: $(call check-melonloader)
# Returns: "installed" if found, "not-installed" if missing, "no-config" if config missing
define check-melonloader
$(shell \
	if [ ! -f "Directory.Build.props" ]; then \
		echo "no-config"; \
	else \
		GAME_DIR=$$(grep -o '<GameAbsoluteDir>.*</GameAbsoluteDir>' Directory.Build.props | sed 's/<GameAbsoluteDir>//g' | sed 's/<\/GameAbsoluteDir>//g' 2>/dev/null); \
		if [ -n "$$GAME_DIR" ] && [ -f "$$GAME_DIR/MelonLoader/net6/MelonLoader.dll" ]; then \
			echo "installed"; \
		else \
			echo "not-installed"; \
		fi; \
	fi \
)
endef

# Function to check if Il2CppAssemblies exist (game has been run)
# Usage: $(call check-assemblies)
# Returns: "ready" if found, "not-ready" if missing, "no-config" if config missing
define check-assemblies
$(shell \
	if [ ! -f "Directory.Build.props" ]; then \
		echo "no-config"; \
	else \
		GAME_DIR=$$(grep -o '<GameAbsoluteDir>.*</GameAbsoluteDir>' Directory.Build.props | sed 's/<GameAbsoluteDir>//g' | sed 's/<\/GameAbsoluteDir>//g' 2>/dev/null); \
		if [ -n "$$GAME_DIR" ] && [ -d "$$GAME_DIR/MelonLoader/Il2CppAssemblies" ]; then \
			echo "ready"; \
		else \
			echo "not-ready"; \
		fi; \
	fi \
)
endef

fix-permissions:
	@chmod +x scripts/* 2>/dev/null || true

conf: fix-permissions
	@./scripts/make-config

clean:
	@rm -rf TDModLoader/bin/ TDModLoader/obj/
	@echo "Cleaned build artifacts"

install-melonloader: fix-permissions
	@./scripts/install-melonloader

setup:
	@echo "Setting up Tower Dominion Mod Loader..."
	@if [ ! -f "Directory.Build.props" ]; then \
		echo "Config missing, running conf target..."; \
		$(MAKE) conf; \
	fi
	@MELONLOADER_STATUS=$(call check-melonloader); \
	if [ "$$MELONLOADER_STATUS" = "not-installed" ]; then \
		echo "MelonLoader missing, installing..."; \
		$(MAKE) install-melonloader; \
	elif [ "$$MELONLOADER_STATUS" = "installed" ]; then \
		echo "MelonLoader already installed"; \
	else \
		echo "Error: Could not determine game directory"; \
	fi
	@echo "Setup complete!"

build:
	@echo "Checking build prerequisites..."
	@ASSEMBLIES_STATUS=$(call check-assemblies); \
	if [ "$$ASSEMBLIES_STATUS" = "no-config" ]; then \
		echo "Error: Directory.Build.props not found. Run 'make conf' first."; \
		exit 1; \
	elif [ "$$ASSEMBLIES_STATUS" = "not-ready" ]; then \
		echo "Error: Il2CppAssemblies not found. Please run the game at least once with MelonLoader installed."; \
		exit 1; \
	fi
	@echo "Building TDModLoader..."
	@cd TDModLoader && dotnet build
	@echo "Build complete!"

dev:
	@echo "Setting up development symlink..."
	@if [ ! -f "Directory.Build.props" ]; then \
		echo "Error: Directory.Build.props not found. Run 'make conf' first."; \
		exit 1; \
	fi
	@GAME_DIR=$$(grep -o '<GameAbsoluteDir>.*</GameAbsoluteDir>' Directory.Build.props | sed 's/<GameAbsoluteDir>//g' | sed 's/<\/GameAbsoluteDir>//g' 2>/dev/null); \
	BUILD_DIR="$$(pwd)/TDModLoader/bin/Debug/net6.0"; \
	MODS_LINK="$$GAME_DIR/Mods"; \
	if [ -z "$$GAME_DIR" ]; then \
		echo "Error: Could not determine game directory from config"; \
		exit 1; \
	fi; \
	if [ ! -d "$$BUILD_DIR" ]; then \
		echo "Error: Build directory not found. Run 'make build' first."; \
		echo "Expected: $$BUILD_DIR"; \
		exit 1; \
	fi; \
	if [ -L "$$MODS_LINK" ]; then \
		echo "Removing existing Mods symlink..."; \
		rm "$$MODS_LINK"; \
	elif [ -d "$$MODS_LINK" ]; then \
		echo "Warning: $$MODS_LINK exists as a directory. Please remove it manually."; \
		exit 1; \
	fi; \
	echo "Creating symlink: $$MODS_LINK -> $$BUILD_DIR"; \
	ln -s "$$BUILD_DIR" "$$MODS_LINK"; \
	echo "Development symlink created successfully!"

watch:
	@echo "Starting watch mode... (Press Ctrl+C to stop)"
	@ASSEMBLIES_STATUS=$(call check-assemblies); \
	if [ "$$ASSEMBLIES_STATUS" = "no-config" ]; then \
		echo "Error: Directory.Build.props not found. Run 'make conf' first."; \
		exit 1; \
	elif [ "$$ASSEMBLIES_STATUS" = "not-ready" ]; then \
		echo "Error: Il2CppAssemblies not found. Please run the game at least once with MelonLoader installed."; \
		exit 1; \
	fi
	@echo "Watching TDModLoader/ for changes..."
	@cd TDModLoader && dotnet watch build --no-hot-reload

shell: fix-permissions
	@./scripts/td-shell

logs:
	@echo "Following MelonLoader logs... (Press Ctrl+C to stop)"
	@if [ ! -f "Directory.Build.props" ]; then \
		echo "Error: Directory.Build.props not found. Run 'make conf' first."; \
		exit 1; \
	fi
	@GAME_DIR=$$(grep -o '<GameAbsoluteDir>.*</GameAbsoluteDir>' Directory.Build.props | sed 's/<GameAbsoluteDir>//g' | sed 's/<\/GameAbsoluteDir>//g' 2>/dev/null); \
	if [ -z "$$GAME_DIR" ]; then \
		echo "Error: Could not determine game directory from config"; \
		exit 1; \
	fi; \
	LOG_FILE="$$GAME_DIR/MelonLoader/Latest.log"; \
	if [ ! -f "$$LOG_FILE" ]; then \
		echo "Error: Log file not found: $$LOG_FILE"; \
		echo "Make sure MelonLoader is installed and the game has been run at least once."; \
		exit 1; \
	fi; \
	echo "Tailing: $$LOG_FILE"; \
	tail -f "$$LOG_FILE"

uninstall:
	@echo "Uninstalling MelonLoader..."
	@MELONLOADER_STATUS=$(call check-melonloader); \
	if [ "$$MELONLOADER_STATUS" = "no-config" ]; then \
		echo "Error: Directory.Build.props not found. Run 'make conf' first."; \
	elif [ "$$MELONLOADER_STATUS" = "installed" ]; then \
		GAME_DIR=$$(grep -o '<GameAbsoluteDir>.*</GameAbsoluteDir>' Directory.Build.props | sed 's/<GameAbsoluteDir>//g' | sed 's/<\/GameAbsoluteDir>//g' 2>/dev/null); \
		echo "Removing MelonLoader from: $$GAME_DIR"; \
		rm -rf "$$GAME_DIR/MelonLoader"; \
		rm -f "$$GAME_DIR/version.dll"; \
		rm -f "$$GAME_DIR/dobby.dll"; \
		echo "MelonLoader uninstalled successfully!"; \
	else \
		echo "MelonLoader not installed"; \
	fi