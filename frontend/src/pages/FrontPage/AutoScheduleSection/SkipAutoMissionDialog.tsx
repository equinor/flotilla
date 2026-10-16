import { Button, Typography } from '@equinor/eds-core-react'
import { useState } from 'react'
import { useBackendApi } from 'api/UseBackendApi'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { useAlertContext } from 'contexts/AlertContext'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'contexts/MissionDefinitionsContext'
import { useNow } from 'hooks/useNow'
import { AutoScheduleOccurrence, canSkipOccurrence, formatScheduledAt } from 'models/AutoScheduleOccurrence'

export const SkipAutoMissionDialog = ({
    occurrence,
    onClose,
}: {
    occurrence: AutoScheduleOccurrence
    onClose: () => void
}) => {
    const api = useBackendApi()
    const { TranslateText, language } = useLanguageContext()
    const { setBanner } = useAlertContext()
    const { autoScheduleOccurrences, autoScheduleError, refreshOccurrences } = useMissionDefinitionsContext()
    const [submitting, setSubmitting] = useState(false)
    const now = useNow()
    const current = autoScheduleOccurrences.find((item) => item.id === occurrence.id)
    const eligible = !autoScheduleError && current !== undefined && canSkipOccurrence(current, now)

    const skip = async () => {
        setSubmitting(true)
        try {
            await api.skipAutoScheduledMission(occurrence.mission.id, occurrence.id)
            onClose()
        } catch {
            setBanner(TranslateText('Unable to skip this occurrence. Refresh the schedule and try again.'), 'error')
        } finally {
            setSubmitting(false)
            refreshOccurrences()
        }
    }

    return (
        <StyledDialog open onClose={submitting ? undefined : onClose}>
            <StyledDialog.Header>
                <StyledDialog.Title>{TranslateText('SkipAutoMission')}</StyledDialog.Title>
            </StyledDialog.Header>
            <StyledDialog.CustomContent>
                <Typography>
                    {TranslateText('Are you sure you want to skip {0} scheduled for {1} (CET/CEST)?', [
                        occurrence.mission.name,
                        formatScheduledAt(occurrence.scheduledAt, language),
                    ])}
                </Typography>
                {!eligible && <Typography>{TranslateText('This occurrence can no longer be skipped.')}</Typography>}
            </StyledDialog.CustomContent>
            <StyledDialog.Actions>
                <Button onClick={onClose} disabled={submitting} variant="outlined">
                    {TranslateText('Cancel')}
                </Button>
                <Button
                    onClick={() => void skip()}
                    disabled={submitting || !eligible}
                    variant="outlined"
                    color="danger"
                >
                    {TranslateText('SkipAutoMission')}
                </Button>
            </StyledDialog.Actions>
        </StyledDialog>
    )
}
