import { BackendAPICaller } from 'api/ApiCaller'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { StyledButton } from 'components/Styles/StyledComponents'
import styled from 'styled-components'
import { useState } from 'react'

const StyledTextButton = styled(StyledButton)`
    text-align: left;
    align-self: stretch;
`

export const InterventionNeededButton = ({ robot }: { robot: Robot }) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [isDisabled, setIsDisabled] = useState(false)

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

    return (
        <StyledTextButton variant="outlined" disabled={isDisabled} onClick={releaseInterventionNeeded}>
            {TranslateText('Robot ready for missions')}
        </StyledTextButton>
    )
}
