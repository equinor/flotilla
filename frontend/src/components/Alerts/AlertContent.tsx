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

export type AlertContent =
    | { kind: 'failedMissions'; missions: Mission[] }
    | { kind: 'autoScheduleFail'; failedMissions: AutoScheduleFailedMissionDict }
    | { kind: 'dock'; dockType: AlertType }
    | { kind: 'requestFail'; message: string }
    | { kind: 'failure'; title: string; message: string }
    | { kind: 'info'; title: string; message: string }

export const getAlertCategory = (content: AlertContent): AlertCategory => {
    switch (content.kind) {
        case 'dock':
            return content.dockType === AlertType.RequestDock ? AlertCategory.WARNING : AlertCategory.INFO
        case 'info':
            return AlertCategory.INFO
        default:
            return AlertCategory.ERROR
    }
}

export const AlertBannerContent = ({ content }: { content: AlertContent }): ReactElement => {
    const { TranslateText } = useLanguageContext()
    switch (content.kind) {
        case 'failedMissions':
            return <FailedMissionAlertContent missions={content.missions} />
        case 'autoScheduleFail':
            return <AutoScheduleFailContent failedMissions={content.failedMissions} />
        case 'dock':
            return <DockAlertContent alertType={content.dockType} />
        case 'requestFail':
            return (
                <AlertBannerLayout
                    icon={Icons.Failed}
                    iconColor={dangerColor}
                    title={TranslateText('Request error')}
                    message={content.message}
                />
            )
        case 'failure':
            return (
                <AlertBannerLayout
                    icon={Icons.Failed}
                    iconColor={dangerColor}
                    title={content.title}
                    message={content.message}
                />
            )
        case 'info':
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
        case 'failedMissions':
            return <FailedMissionAlertListContent missions={content.missions} />
        case 'autoScheduleFail':
            return <AutoScheduleFailContent failedMissions={content.failedMissions} />
        case 'dock':
            return <DockAlertListContent alertType={content.dockType} />
        case 'requestFail':
            return (
                <AlertListContents
                    icon={Icons.Failed}
                    iconColor={dangerColor}
                    alertTitle={TranslateText('Request error')}
                    alertText={content.message}
                />
            )
        case 'failure':
            return (
                <AlertListContents
                    icon={Icons.Failed}
                    iconColor={dangerColor}
                    alertTitle={TranslateText(content.title)}
                    alertText={TranslateText(content.message)}
                />
            )
        case 'info':
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
