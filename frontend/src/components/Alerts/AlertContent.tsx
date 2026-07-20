import { ReactElement } from 'react'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { Icons } from 'utils/icons'
import { AlertType, AutoScheduleFailedMissionDict } from 'components/Contexts/AlertContext'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { AlertCategory } from './AlertsBanner'
import { AlertListContents } from './AlertsListItem'
import { AlertBannerLayout } from './AlertBannerLayout'
import { AutoScheduleFailContent } from './AutoScheduleFailAlert'
import { FailedMissionAlertContent, FailedMissionAlertListContent } from './FailedMissionAlert'
import { DockAlertContent, DockAlertListContent } from './DockAlert'

const dangerColor = tokens.colors.interactive.danger__resting.hex
const infoColor = tokens.colors.interactive.primary__resting.hex

export enum AlertKind {
    FailedMissions = 'failedMissions',
    AutoScheduleFail = 'autoScheduleFail',
    Dock = 'dock',
    RequestFail = 'requestFail',
    Failure = 'failure',
    Info = 'info',
}

export type AlertContent =
    | { kind: AlertKind.FailedMissions; missions: Mission[] }
    | { kind: AlertKind.AutoScheduleFail; failedMissions: AutoScheduleFailedMissionDict }
    | { kind: AlertKind.Dock; dockType: AlertType }
    | { kind: AlertKind.RequestFail; message: string }
    | { kind: AlertKind.Failure; title: string; message: string }
    | { kind: AlertKind.Info; title: string; message: string }

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

export const AlertBannerContent = ({ content }: { content: AlertContent }): ReactElement => {
    const { TranslateText } = useLanguageContext()
    switch (content.kind) {
        case AlertKind.FailedMissions:
            return <FailedMissionAlertContent missions={content.missions} />
        case AlertKind.AutoScheduleFail:
            return <AutoScheduleFailContent failedMissions={content.failedMissions} />
        case AlertKind.Dock:
            return <DockAlertContent alertType={content.dockType} />
        case AlertKind.RequestFail:
            return (
                <AlertBannerLayout
                    icon={Icons.Failed}
                    iconColor={dangerColor}
                    title={TranslateText('Request error')}
                    message={content.message}
                />
            )
        case AlertKind.Failure:
            return (
                <AlertBannerLayout
                    icon={Icons.Failed}
                    iconColor={dangerColor}
                    title={content.title}
                    message={content.message}
                />
            )
        case AlertKind.Info:
            return (
                <AlertBannerLayout
                    icon={Icons.Info}
                    iconColor={infoColor}
                    title={content.title}
                    message={content.message}
                />
            )
    }
}

export const AlertListItemContent = ({ content }: { content: AlertContent }): ReactElement => {
    const { TranslateText } = useLanguageContext()
    switch (content.kind) {
        case AlertKind.FailedMissions:
            return <FailedMissionAlertListContent missions={content.missions} />
        case AlertKind.AutoScheduleFail:
            return <AutoScheduleFailContent failedMissions={content.failedMissions} />
        case AlertKind.Dock:
            return <DockAlertListContent alertType={content.dockType} />
        case AlertKind.RequestFail:
            return (
                <AlertListContents
                    icon={Icons.Failed}
                    iconColor={dangerColor}
                    alertTitle={TranslateText('Request error')}
                    alertText={content.message}
                />
            )
        case AlertKind.Failure:
            return (
                <AlertListContents
                    icon={Icons.Failed}
                    iconColor={dangerColor}
                    alertTitle={TranslateText(content.title)}
                    alertText={TranslateText(content.message)}
                />
            )
        case AlertKind.Info:
            return (
                <AlertListContents
                    icon={Icons.Info}
                    iconColor={infoColor}
                    alertTitle={content.title}
                    alertText={content.message}
                />
            )
    }
}
