# 历史粘贴板 — Swift 版本构建脚本
# 用法：
#   make          — debug 构建并运行
#   make release  — release 构建
#   make app      — 创建 .app 捆绑包
#   make dmg      — 创建 DMG 安装镜像（发布用）
#   make clean    — 清理构建产物
#   make run      — 构建并运行

APP_NAME       := ClipboardManager
DISPLAY_NAME   := 历史粘贴板
BUNDLE_ID      := com.clipboardmanager.app
BUILD_DIR      := .build
RELEASE_DIR    := $(BUILD_DIR)/release
APP_DIR        := $(BUILD_DIR)/$(APP_NAME).app
CONTENTS_DIR   := $(APP_DIR)/Contents
MACOS_DIR      := $(CONTENTS_DIR)/MacOS
RESOURCES_DIR  := $(CONTENTS_DIR)/Resources
SWIFT_FLAGS    := -c release
ICONSET_DIR    := $(BUILD_DIR)/AppIcon.iconset
VERSION        := 1.0.3
BUILD          := 3
VOLUME_NAME    := ClipboardManager
DMG_NAME       := $(APP_NAME)-$(VERSION).dmg

.PHONY: all build release app clean run xcode dmg icns install

# ── 默认：debug 构建 ──────────────────────────────

all: build

build:
	swift build

# ── Release 构建 ──────────────────────────────────

release:
	swift build $(SWIFT_FLAGS)

# ── 运行 ──────────────────────────────────────────

run: build
	swift run

# ── 生成应用图标 (.icns) ─────────────────────────

icns:
	@echo "🖼️  生成 .icns 图标..."
	@rm -rf "$(BUILD_DIR)/AppIcon.iconset"
	@mkdir -p "$(BUILD_DIR)/AppIcon.iconset"
	@# 需要一张 1024x1024 的 icon.png 作为源文件
	@if [ ! -f Resources/icon.png ]; then \
		echo "❌ 缺少 Resources/icon.png（请放入 1024x1024 的 PNG 图标）"; \
		exit 1; \
	fi
	@# 生成各尺寸图标
	@sips -z 16 16   Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_16x16.png" > /dev/null 2>&1
	@sips -z 32 32   Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_16x16@2x.png" > /dev/null 2>&1
	@sips -z 32 32   Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_32x32.png" > /dev/null 2>&1
	@sips -z 64 64   Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_32x32@2x.png" > /dev/null 2>&1
	@sips -z 128 128 Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_128x128.png" > /dev/null 2>&1
	@sips -z 256 256 Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_128x128@2x.png" > /dev/null 2>&1
	@sips -z 256 256 Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_256x256.png" > /dev/null 2>&1
	@sips -z 512 512 Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_256x256@2x.png" > /dev/null 2>&1
	@sips -z 512 512 Resources/icon.png --out "$(BUILD_DIR)/AppIcon.iconset/icon_512x512.png" > /dev/null 2>&1
	@cp Resources/icon.png "$(BUILD_DIR)/AppIcon.iconset/icon_512x512@2x.png"
	@# 生成 .icns
	@iconutil -c icns "$(BUILD_DIR)/AppIcon.iconset" -o "$(BUILD_DIR)/AppIcon.icns"
	@rm -rf "$(BUILD_DIR)/AppIcon.iconset"
	@echo "✅ AppIcon.icns 已生成"

# ── 创建 .app 捆绑包 ──────────────────────────────

app: release icns
	@echo "📦 创建 .app 捆绑包..."
	@rm -rf "$(APP_DIR)"
	@mkdir -p "$(MACOS_DIR)"
	@mkdir -p "$(RESOURCES_DIR)"

	# 复制可执行文件
	@cp "$(RELEASE_DIR)/$(APP_NAME)" "$(MACOS_DIR)/"

	# 复制 Info.plist（注入版本号）
	@sed 's/__VERSION__/$(VERSION)/g; s/__BUILD__/$(BUILD)/g' \
		Resources/Info.plist > "$(CONTENTS_DIR)/Info.plist"

	# 复制图标
	@cp "$(BUILD_DIR)/AppIcon.icns" "$(RESOURCES_DIR)/"

	# 设置可执行权限
	@chmod +x "$(MACOS_DIR)/$(APP_NAME)"

	@echo ""
	@echo "✅ .app 捆绑包已创建: $(APP_DIR)"
	@echo "   双击运行: open $(APP_DIR)"

# ── 创建 DMG 安装镜像 ─────────────────────────────

dmg: app
	@echo "💿 创建 DMG 安装镜像..."
	@rm -f "$(BUILD_DIR)/$(DMG_NAME)" "$(BUILD_DIR)/tmp.dmg"
	@echo "  创建空白 DMG..."
	@hdiutil create -volname "$(VOLUME_NAME)" \
		-size 50m \
		-layout NONE \
		-fs "APFS" \
		-ov \
		"$(BUILD_DIR)/tmp.dmg" > /dev/null
	@echo "  挂载并复制文件..."
	@MOUNT_POINT=$$(hdiutil attach "$(BUILD_DIR)/tmp.dmg" -nobrowse 2>&1 | tr '\t' ' ' | grep '/Volumes/' | tail -1 | awk '{print $$NF}'); \
	echo "  挂载点: $$MOUNT_POINT"; \
	cp -R "$(APP_DIR)" "$$MOUNT_POINT/$(APP_NAME).app"; \
	ln -sf /Applications "$$MOUNT_POINT/Applications"; \
	rm -f "$$MOUNT_POINT/.DS_Store"; \
	sync; sleep 1; \
	echo "  设置布局..."; \
	osascript -e "tell application \"Finder\"" \
		-e "  activate" \
		-e "  set volPath to \"$$MOUNT_POINT\" as POSIX file as alias" \
		-e "  open volPath" \
		-e "  delay 1" \
		-e "  set frontWin to front window" \
		-e "  set current view of frontWin to icon view" \
		-e "  set toolbar visible of frontWin to false" \
		-e "  set statusbar visible of frontWin to false" \
		-e "  set bounds of frontWin to {400, 400, 920, 680}" \
		-e "  set viewOpts to icon view options of frontWin" \
		-e "  set arrangement of viewOpts to not arranged" \
		-e "  set icon size of viewOpts to 72" \
		-e "  try" \
		-e "    set position of item \"$(APP_NAME).app\" of frontWin to {160, 120}" \
		-e "  end try" \
		-e "  try" \
		-e "    set position of item \"Applications\" of frontWin to {360, 120}" \
		-e "  end try" \
		-e "  delay 0.5" \
		-e "  close frontWin" \
		-e "end tell" 2>&1 || echo "  ⚠️  布局设置失败，使用默认排列"; \
	sleep 0.5; \
	echo "  卸载 DMG..."; \
	hdiutil detach "$$MOUNT_POINT" -force > /dev/null 2>&1
	@echo "  转换为压缩格式..."
	@hdiutil convert "$(BUILD_DIR)/tmp.dmg" -format UDZO -o "$(BUILD_DIR)/$(DMG_NAME)" > /dev/null
	@rm -f "$(BUILD_DIR)/tmp.dmg"
	@echo ""
	@echo "✅ DMG 已创建: $(BUILD_DIR)/$(DMG_NAME)"
	@echo "   大小: $$(du -h $(BUILD_DIR)/$(DMG_NAME) | cut -f1)"

# ── 清理 ──────────────────────────────────────────

clean:
	swift package clean
	rm -rf "$(APP_DIR)"
	rm -f "$(BUILD_DIR)/$(DMG_NAME)"

# ── 生成 Xcode 项目（开发用） ─────────────────────

xcode:
	@echo "👉 在 Xcode 中打开本项目的 Package.swift 即可："
	@echo "   open Package.swift"
	@echo ""
	@echo "   或者通过菜单：File → Open → 选择 Package.swift"

# ── 安装到 /Applications ──────────────────────────

install: app
	@echo "📥 安装到 /Applications..."
	@rm -rf "/Applications/$(APP_NAME).app"
	@cp -R "$(APP_DIR)" "/Applications/$(APP_NAME).app"
	@echo "✅ 已安装到 /Applications/$(APP_NAME).app"

# ── 代码签名（需要 Apple Developer 证书） ──────────

sign:
	@echo "🔐 代码签名..."
	@codesign --force --deep --sign "Developer ID Application" "$(APP_DIR)"
	@echo "✅ 已签名"

notarize: sign
	@echo "📤 提交公证..."
	@ditto -c -k --keepParent "$(APP_DIR)" "$(BUILD_DIR)/$(APP_NAME).zip"
	@xcrun notarytool submit "$(BUILD_DIR)/$(APP_NAME).zip" \
		--apple-id "$(APPLE_ID)" \
		--team-id "$(TEAM_ID)" \
		--password "$(APP_PASSWORD)" \
		--wait
	@echo "✅ 公证完成"
	@# 装订公证票据
	@xcrun stapler staple "$(APP_DIR)"
	@echo "✅ 公证票据已装订"

# ── 开发辅助 ──────────────────────────────────────

# 查看数据库内容
db-view:
	sqlite3 ~/Library/Application\ Support/ClipboardManager/history.db \
		"SELECT id, content_type, substr(text_content,1,40), created_at, is_pinned FROM clipboard_items ORDER BY id DESC LIMIT 20;"

# 数据库统计
db-stats:
	sqlite3 ~/Library/Application\ Support/ClipboardManager/history.db \
		"SELECT content_type, COUNT(*) FROM clipboard_items GROUP BY content_type; SELECT COUNT(*) AS total FROM clipboard_items;"

# 清理数据库（慎用！）
db-clean:
	rm -f ~/Library/Application\ Support/ClipboardManager/history.db
	rm -rf ~/Library/Application\ Support/ClipboardManager/images/
