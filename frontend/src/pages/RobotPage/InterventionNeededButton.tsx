import { BackendAPICaller } from 'api/ApiCaller'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { StyledButton, StyledDialog } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { useState } from 'react'
import { Button, Typography } from '@equinor/eds-core-react'

const StyledTextButton = styled(StyledButton)`
    text-align: left;
    align-self: stretch;
`

export const InterventionNeededButton = ({ robot }: { robot: Robot }) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [isDisabled, setIsDisabled] = useState(false)
    const [isDialogOpen, setIsDialogOpen] = useState(false)

    const releaseInterventionNeeded = () => {
        disableButton()

        BackendAPICaller.releaseInterventionNeeded(robot.id).catch(() => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent
                    translatedMessage={TranslateText('Unable to release robot {0} from intervention mode', [
                        robot.name ?? '',
                    ])}
                />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent
                    translatedMessage={TranslateText('Unable to release robot {0} from intervention mode', [
                        robot.name ?? '',
                    ])}
                />,
                AlertCategory.ERROR
            )
        })
    }

    const disableButton = () => {
        setIsDisabled(true)
        setTimeout(() => {
            setIsDisabled(false)
        }, 1000)
    }

    const onConfirm = () => {
        releaseInterventionNeeded()
        setIsDialogOpen(false)
    }

    return (
        <>
            <StyledTextButton variant="outlined" disabled={isDisabled} onClick={() => setIsDialogOpen(true)}>
                {TranslateText('intervention_needed_button_text')}
            </StyledTextButton>
            <ReleaseInterventionNeededDialog
                isOpen={isDialogOpen}
                onClose={() => setIsDialogOpen(false)}
                onConfirm={onConfirm}
            />
        </>
    )
}

interface ReleaseInterventionNeededDialogProps {
    isOpen: boolean
    onClose: () => void
    onConfirm: () => void
}

const ReleaseInterventionNeededDialog = ({ isOpen, onClose, onConfirm }: ReleaseInterventionNeededDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDialog open={isOpen}>
            <StyledDialog.CustomContent>
                <Typography variant="body_short">{TranslateText('intervention_needed_dialog_text')}</Typography>
            </StyledDialog.CustomContent>
            <StyledDialog.Actions>
                <Button color="danger" onClick={onConfirm}>
                    {TranslateText('confirm_word')}
                </Button>
                <Button onClick={onClose} variant="outlined">
                    {TranslateText('close_word')}
                </Button>
            </StyledDialog.Actions>
        </StyledDialog>
    )
}
