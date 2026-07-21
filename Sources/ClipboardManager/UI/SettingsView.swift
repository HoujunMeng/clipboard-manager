import SwiftUI

/// 设置弹窗
struct SettingsView: View {
    @Environment(\.dismiss) private var dismiss

    @State private var retentionDaysIndex: Int
    @State private var launchAtLogin: Bool

    private let dayOptions = [1, 3, 5]
    private let settings = SettingsManager.shared

    init() {
        let days = SettingsManager.shared.retentionDays
        let index = [1, 3, 5].firstIndex(of: days) ?? 1
        _retentionDaysIndex = State(initialValue: index)
        _launchAtLogin = State(initialValue: SettingsManager.shared.launchAtLogin)
    }

    var body: some View {
        VStack(spacing: 0) {
            // 标题栏
            HStack {
                Text("设置")
                    .font(.system(size: 14, weight: .semibold))
                    .foregroundColor(.textPrimary)
                Spacer()
            }
            .padding(.horizontal, 20)
            .padding(.top, 20)
            .padding(.bottom, 16)

            Divider()

            // 内容区
            VStack(spacing: 20) {
                // 保留天数
                HStack(spacing: 12) {
                    Text("保留天数：")
                        .font(.system(size: 13))
                        .foregroundColor(.textPrimary)

                    Picker("", selection: $retentionDaysIndex) {
                        Text("1 天").tag(0)
                        Text("3 天").tag(1)
                        Text("5 天").tag(2)
                    }
                    .pickerStyle(.segmented)
                    .frame(width: 150)

                    Spacer()
                }

                // 开机启动
                HStack {
                    Toggle("开机自动启动", isOn: $launchAtLogin)
                        .font(.system(size: 13))
                        .foregroundColor(.textPrimary)
                    Spacer()
                }
            }
            .padding(.horizontal, 20)
            .padding(.vertical, 20)

            Divider()

            // 按钮栏
            HStack {
                Spacer()

                Button("取消") {
                    dismiss()
                }
                .keyboardShortcut(.cancelAction)

                Button("保存") {
                    saveSettings()
                    dismiss()
                }
                .keyboardShortcut(.defaultAction)
            }
            .padding(.horizontal, 20)
            .padding(.bottom, 16)
            .padding(.top, 12)
        }
        .frame(width: 320)
    }

    private func saveSettings() {
        let newDays = dayOptions[retentionDaysIndex]
        let oldDays = settings.retentionDays

        settings.retentionDays = newDays
        settings.launchAtLogin = launchAtLogin

        // 保留天数变更时触发清理
        if newDays != oldDays {
            DatabaseService.shared.cleanup(retentionDays: newDays)
        }
    }
}
