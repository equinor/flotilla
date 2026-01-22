import { BackendAPICaller } from 'api/ApiCaller'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotWithoutTelemetry } from 'models/Robot'
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

export const ReturnHomeButton = ({ robot }: { robot: RobotWithoutTelemetry }) => {
    const { TranslateText } = useLanguageContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [isDisabled, setIsDisabled] = useState(false)

    const returnRobotToHome = () => {
        disableButton()

        BackendAPICaller.returnRobotToHome(robot.id).catch(() => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent
                    translatedMessage={TranslateText('Failed to send robot {0} home', [robot.name ?? ''])}
                />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent
                    translatedMessage={TranslateText('Failed to send robot {0} home', [robot.name ?? ''])}
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
        <StyledTextButton variant="outlined" disabled={isDisabled} onClick={returnRobotToHome}>
            {TranslateText('Return robot to home')}
        </StyledTextButton>
    )
}
