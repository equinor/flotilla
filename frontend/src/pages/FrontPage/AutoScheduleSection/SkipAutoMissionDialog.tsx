import { Button, Typography } from '@equinor/eds-core-react'
import { useState } from 'react'
import { useBackendApi } from 'api/UseBackendApi'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { useAlertContext } from 'contexts/AlertContext'
import { useLanguageContext } from 'contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'contexts/MissionDefinitionsContext'
import { useNow } from 'hooks/useNow'
import {
    AutoScheduleOccurrence,
    canSkipOccurrence,
    canUnskipOccurrence,
    formatScheduledAt,
} from 'models/AutoScheduleOccurrence'

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
    const undoSkip = occurrence.status === 'Skipped'
    const actionLabel = undoSkip ? 'Undo skip' : 'SkipAutoMission'
    const current = autoScheduleOccurrences.find((item) => item.id === occurrence.id)
    const eligible =
        !autoScheduleError &&
        current !== undefined &&
        (undoSkip ? canUnskipOccurrence(current, now) : canSkipOccurrence(current, now))

    const submit = async () => {
        setSubmitting(true)
        try {
            if (undoSkip) await api.unskipAutoScheduledMission(occurrence.mission.id, occurrence.id)
            else await api.skipAutoScheduledMission(occurrence.mission.id, occurrence.id)
            onClose()
        } catch {
            setBanner(
                TranslateText(
                    undoSkip
                        ? 'Unable to undo this skip. Refresh the schedule and try again.'
                        : 'Unable to skip this occurrence. Refresh the schedule and try again.'
                ),
                'error'
            )
        } finally {
            setSubmitting(false)
            refreshOccurrences()
        }
    }

    return (
        <StyledDialog open onClose={submitting ? undefined : onClose}>
            <StyledDialog.Header>
                <StyledDialog.Title>{TranslateText(actionLabel)}</StyledDialog.Title>
            </StyledDialog.Header>
            <StyledDialog.CustomContent>
                <Typography>
                    {TranslateText(
                        undoSkip
                            ? 'Restore {0} scheduled for {1} (CET/CEST)? It will become eligible to run at that time.'
                            : 'Are you sure you want to skip {0} scheduled for {1} (CET/CEST)?',
                        [occurrence.mission.name, formatScheduledAt(occurrence.scheduledAt, language)]
                    )}
                </Typography>
                {!eligible && (
                    <Typography>
                        {TranslateText(
                            undoSkip
                                ? 'This skip can no longer be undone.'
                                : 'This occurrence can no longer be skipped.'
                        )}
                    </Typography>
                )}
            </StyledDialog.CustomContent>
            <StyledDialog.Actions>
                <Button onClick={onClose} disabled={submitting} variant="outlined">
                    {TranslateText('Cancel')}
                </Button>
                <Button
                    onClick={() => void submit()}
                    disabled={submitting || !eligible}
                    variant="outlined"
                    color={undoSkip ? 'primary' : 'danger'}
                >
                    {TranslateText(actionLabel)}
                </Button>
            </StyledDialog.Actions>
        </StyledDialog>
    )
}
