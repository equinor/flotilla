import { Button, Popover, Typography } from '@equinor/eds-core-react'
import { useState, useEffect, useRef } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { tokens } from '@equinor/eds-tokens'

interface RobotProps {
    robot: Robot
    armPosition: string
    isRobotAvailable: boolean
}
const feedbackTimer = 10000 // Clear feedback after 10 seconds

const StyledButton = styled.div`
    display: flex;
    justifycontent: center;
    margintop: auto;
`

export function MoveRobotArm({ robot, armPosition, isRobotAvailable }: RobotProps) {
    const { TranslateText } = useLanguageContext()
    const [feedback, setFeedback] = useState('')
    const [usable, setUsable] = useState(!!isRobotAvailable)
    const [isPopoverOpen, setIsPopoverOpen] = useState<boolean>(false)
    const anchorRef = useRef<HTMLButtonElement>(null)

    useEffect(() => {
        setUsable(isRobotAvailable)
    }, [isRobotAvailable])

    const moveArmButtonStyle = () => {
        if (!usable)
            return {
                backgroundColor: tokens.colors.interactive.disabled__fill.hex,
                color: tokens.colors.interactive.disabled__text.hex,
                borderColor: tokens.colors.interactive.disabled__border.hex,
                cursor: 'not-allowed',
            }
    }
    const openPopover = () => {
        setIsPopoverOpen(true)
    }
    const closePopover = () => {
        setIsPopoverOpen(false)
    }

    const onClickMoveArm = () => {
        BackendAPICaller.setArmPosition(robot.id, armPosition)
            .then(() => {
                setUsable(false)
                setFeedback(() => TranslateText('Moving arm to ') + TranslateText(armPosition))
                setTimeout(() => {
                    setFeedback('')
                    setUsable(true)
                }, feedbackTimer)
            })
            .catch((error) => {
                setFeedback(
                    () =>
                        TranslateText('Error moving robot arm to ') +
                        TranslateText(armPosition) +
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
            <Popover anchorEl={anchorRef.current} onClose={closePopover} open={isPopoverOpen} placement="top">
                <Popover.Content>
                    <Typography variant="body_short">
                        {TranslateText(
                            'This button is disabled because the robot is not available. Check that the robot is on, and are not doing any other activities.'
                        )}
                    </Typography>
                    <StyledButton>
                        <Button onClick={closePopover}>{TranslateText('Close')}</Button>
                    </StyledButton>
                </Popover.Content>
            </Popover>
            <Button style={moveArmButtonStyle()} onClick={!usable ? openPopover : onClickMoveArm} ref={anchorRef}>
                {TranslateText('Set robot arm to ') + '"' + TranslateText(armPosition) + '"'}
            </Button>
            {feedback && <p>{feedback}</p>}
        </>
    )
}
