import { AlertCategory } from './AlertsBanner'
import { AlertType, AlertKind, type AlertContent } from 'models/Alert'

export const getAlertCategory = (content: AlertContent): AlertCategory => {
    switch (content.kind) {
        case AlertKind.Dock:
            return content.dockType === AlertType.RequestDock ? AlertCategory.WARNING : AlertCategory.INFO
        case AlertKind.Info:
            return AlertCategory.INFO
        default:
            return AlertCategory.ERROR
    }
}
