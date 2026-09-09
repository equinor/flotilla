import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { useState } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { useAlertContext } from 'components/Contexts/AlertContext'
import { AlertType, AlertKind } from 'models/Alert'
import { StyledDialog as ConfirmDialog } from 'components/Styles/StyledComponents'
import { Feedback } from 'models/InspectionRecord'

const FEEDBACK_ICON_SIZE = 24

const FeedbackSection = styled.div`
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--eds-spacing-horizontal-md);
    padding: var(--eds-spacing-vertical-md) var(--eds-spacing-horizontal-md);
`

const FeedbackPrompt = styled.div`
    display: flex;
    flex-direction: column;
    gap: var(--eds-spacing-vertical-2xs);
`

const FeedbackButtons = styled.div`
    display: flex;
    flex-shrink: 0;
    gap: var(--eds-spacing-horizontal-sm);
`

const FeedbackButton = styled(Button)`
    color: var(--eds-color-text-subtle);
`

const ThumbsUpButton = styled(FeedbackButton)`
    &[aria-pressed='true'] {
        color: var(--eds-color-text-success-subtle);
        background: var(--eds-color-bg-success-fill-muted-default);

        &:hover {
            background: var(--eds-color-bg-success-fill-muted-hover);
        }
        &:active {
            background: var(--eds-color-bg-success-fill-muted-active);
        }
    }
`

const ThumbsDownButton = styled(FeedbackButton)`
    &[aria-pressed='true'] {
        color: var(--eds-color-text-danger-subtle);
        background: var(--eds-color-bg-danger-fill-muted-default);

        &:hover {
            background: var(--eds-color-bg-danger-fill-muted-hover);
        }
        &:active {
            background: var(--eds-color-bg-danger-fill-muted-active);
        }
    }
`

// Mirrors the two operations SARA exposes: PUT { isCorrect } and DELETE.
type PendingAction = { type: 'save'; isCorrect: boolean } | { type: 'remove' }

interface AnalysisFeedbackProps {
    inspectionId: string
    analysisRunId: string
    feedback?: Feedback
}

export const AnalysisFeedback = ({ inspectionId, analysisRunId, feedback }: AnalysisFeedbackProps) => {
    const { TranslateText } = useLanguageContext()
    const { setFeedback, removeFeedback } = useInspectionsContext()
    const { raiseAlert } = useAlertContext()
    const [pendingAction, setPendingAction] = useState<PendingAction | undefined>(undefined)
    const [isSubmitting, setIsSubmitting] = useState(false)

    // Clicking the thumb that is already active removes the feedback instead of setting
    // it again.
    const requestFeedback = (isCorrect: boolean) =>
        setPendingAction(feedback?.isCorrect === isCorrect ? { type: 'remove' } : { type: 'save', isCorrect })

    const onConfirm = () => {
        if (!pendingAction) return

        setIsSubmitting(true)
        setPendingAction(undefined)

        const request =
            pendingAction.type === 'remove'
                ? removeFeedback(inspectionId, analysisRunId)
                : setFeedback(inspectionId, analysisRunId, pendingAction.isCorrect)

        request
            .catch(() => {
                raiseAlert(AlertType.RequestFail, {
                    kind: AlertKind.RequestFail,
                    message: TranslateText('Failed to update the feedback'),
                })
            })
            .finally(() => setIsSubmitting(false))
    }

    return (
        <>
            <FeedbackSection>
                <FeedbackPrompt>
                    <Typography variant="h6">{TranslateText('Feedback')}</Typography>
                    <Typography variant="body_short">{TranslateText('Feedback description')}</Typography>
                </FeedbackPrompt>
                <FeedbackButtons>
                    <ThumbsUpButton
                        variant="ghost_icon"
                        aria-pressed={feedback?.isCorrect === true}
                        disabled={isSubmitting}
                        onClick={() => requestFeedback(true)}
                    >
                        <Icon name={Icons.ThumbsUp} size={FEEDBACK_ICON_SIZE} />
                    </ThumbsUpButton>
                    <ThumbsDownButton
                        variant="ghost_icon"
                        aria-pressed={feedback?.isCorrect === false}
                        disabled={isSubmitting}
                        onClick={() => requestFeedback(false)}
                    >
                        <Icon name={Icons.ThumbsDown} size={FEEDBACK_ICON_SIZE} />
                    </ThumbsDownButton>
                </FeedbackButtons>
            </FeedbackSection>
            <ConfirmFeedbackDialog
                pendingAction={pendingAction}
                onClose={() => setPendingAction(undefined)}
                onConfirm={onConfirm}
            />
        </>
    )
}

interface ConfirmFeedbackDialogProps {
    pendingAction: PendingAction | undefined
    onClose: () => void
    onConfirm: () => void
}

const ConfirmFeedbackDialog = ({ pendingAction, onClose, onConfirm }: ConfirmFeedbackDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <ConfirmDialog open={pendingAction !== undefined} isDismissable onClose={onClose}>
            <ConfirmDialog.Header>
                <ConfirmDialog.Title>{TranslateText('Feedback is shared')}</ConfirmDialog.Title>
            </ConfirmDialog.Header>
            <ConfirmDialog.CustomContent>
                <Typography variant="body_short">
                    {pendingAction?.type === 'remove'
                        ? TranslateText('Retract feedback dialog text')
                        : TranslateText('Save feedback dialog text')}
                </Typography>
            </ConfirmDialog.CustomContent>
            <ConfirmDialog.Actions>
                <Button color={pendingAction?.type === 'remove' ? 'danger' : 'primary'} onClick={onConfirm}>
                    {TranslateText('confirm_word')}
                </Button>
                <Button onClick={onClose} variant="outlined">
                    {TranslateText('close_word')}
                </Button>
            </ConfirmDialog.Actions>
        </ConfirmDialog>
    )
}
