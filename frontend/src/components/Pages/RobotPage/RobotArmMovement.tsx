import { Button, Popover, Typography } from '@equinor/eds-core-react'
import { useState, useEffect, useRef } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Robot } from 'models/Robot'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { StyledButton, StyledDialog } from 'components/Styles/StyledComponents'

interface RobotProps {
    robot: Robot
    armPosition: string
    isRobotAvailable: boolean
}

const StyledCloseButton = styled.div`
    display: flex;
    justify-content: end;
    margin-top: 8px;
`
const LimitWidthStyle = styled.div`
    max-width: calc(80vw);
`

const StyledTextButton = styled(StyledButton)`
    text-align: left;
    padding: 8px;
    width: 8rem;
`

export const MoveRobotArm = ({ robot, armPosition, isRobotAvailable }: RobotProps) => {
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
                setFeedback(() => TranslateText('Moving arm to ') + TranslateText(armPosition))
            })
            .catch((error) => {
                setFeedback(
                    () =>
                        TranslateText('Error moving robot arm to ') +
                        TranslateText(armPosition) +
                        TranslateText(' Error message: ') +
                        error.message
                )
            })
    }
    return (
        <>
            <Popover anchorEl={anchorRef.current} onClose={closePopover} open={isPopoverOpen} placement="top">
                <Popover.Content>
                    <LimitWidthStyle>
                        <Typography variant="body_short">
                            {TranslateText(
                                'This button is disabled because the robot is not available. Check that the robot is on, and are not doing any other activities.'
                            )}
                        </Typography>
                        <StyledCloseButton>
                            <Button onClick={closePopover}>{TranslateText('Close')}</Button>
                        </StyledCloseButton>
                    </LimitWidthStyle>
                </Popover.Content>
            </Popover>
            <StyledTextButton
                style={moveArmButtonStyle()}
                onClick={!usable ? openPopover : onClickMoveArm}
                ref={anchorRef}
            >
                {TranslateText(armPosition)}
            </StyledTextButton>
            <StyledDialog open={feedback !== ''}>
                {feedback}
                <StyledCloseButton>
                    <Button onClick={() => setFeedback('')}>{TranslateText('Close')}</Button>
                </StyledCloseButton>
            </StyledDialog>
        </>
    )
}
