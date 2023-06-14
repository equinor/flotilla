import { Button } from '@equinor/eds-core-react'
import { useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { TranslateText } from 'components/Contexts/LanguageContext'

interface RobotProps {
    robot: Robot
    armPosition: string
}
const feedbackTimer = 10000 // Clear feedback after 10 seconds

export function MoveRobotArm({ robot, armPosition }: RobotProps) {
    const [feedback, setFeedback] = useState('')
    const onClickMoveArm = () => {
        BackendAPICaller.setArmPosition(robot.id, armPosition)
            .then(() => {
                setFeedback(() => TranslateText('Moving arm to ') + armPosition)
                setTimeout(() => {
                    setFeedback('')
                }, feedbackTimer)
            })
            .catch((error) => {
                setFeedback(
                    () =>
                        TranslateText('Error moving robot arm to ') +
                        armPosition +
                        TranslateText(' Error message: ') +
                        error.message
                )
                setTimeout(() => {
                    setFeedback('')
                }, feedbackTimer)
            })
    }
    return (
        <>
            <Button onClick={onClickMoveArm}>{TranslateText('Set robot arm to ') + '"' + armPosition + '"'}</Button>
            {feedback && <p>{feedback}</p>}
        </>
    )
}
