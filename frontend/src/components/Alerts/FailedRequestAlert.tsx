import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { AlertListContents } from './AlertsListItem'
import { FailedAlertContent } from './FailedAlertContent'

export const FailedRequestAlertContent = ({ translatedMessage }: { translatedMessage: string }) => {
    const { TranslateText } = useLanguageContext()
    return <FailedAlertContent title={TranslateText('Request error')} message={translatedMessage} />
}

export const FailedRequestAlertListContent = ({ translatedMessage }: { translatedMessage: string }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <AlertListContents
            icon={Icons.Failed}
            alertTitle={TranslateText('Request error')}
            alertText={translatedMessage}
            iconColor={tokens.colors.interactive.danger__resting.hex}
        />
    )
}
