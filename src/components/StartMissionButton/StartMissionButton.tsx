import { Button, Icon, Popover, SingleSelect } from '@equinor/eds-core-react'
import { play_circle } from '@equinor/eds-icons'
import { Mission } from 'models/mission'
import { Robot, RobotStatus } from 'models/robot'
import { UseComboboxStateChange } from 'downshift'
import { useRef, useState } from 'react'
import styles from './startMissionButton.module.css'

interface StartMissionButtonProps {
    mission: Mission
    robots: Robot[]
}

Icon.add({ play_circle })

interface StartMissionSelectProps {
    robots: Robot[]
}

const StartMissionSelect = ({ robots }: StartMissionSelectProps): JSX.Element => {
    const [selectedRobot, setSelectedRobot] = useState<Robot>()

    const handleSelectedItemChange = (changes: UseComboboxStateChange<string>) => {
        const robot: Robot = robots.filter(function (robot) {
            return robot.robotInfo.name === changes.selectedItem
        })[0]
        setSelectedRobot(robot)
    }

    const availableRobots = robots
        .filter(function (robot) {
            return robot.status === RobotStatus.Available
        })
        .map(function (robot) {
            return robot.robotInfo.name
        })

    const onClick = () => {}

    return (
        <>
            <SingleSelect
                items={availableRobots}
                label="Select Robot"
                handleSelectedItemChange={handleSelectedItemChange}
                onSelect={onClick}
            />
            <Button variant="ghost_icon">
                <Icon name="play_circle" size={24} />
            </Button>
        </>
    )
}

const StartMissionButton = ({ mission, robots }: StartMissionButtonProps): JSX.Element => {
    const [isOpen, setIsOpen] = useState<boolean>(false)
    const anchorRef = useRef<HTMLButtonElement>(null)

    const closePopover = () => setIsOpen(false)
    const toggleOpen = () => setIsOpen(!isOpen)

    return (
        <>
            <Button ref={anchorRef} variant="ghost_icon" onClick={toggleOpen}>
                <Icon name="play_circle" size={24} />
            </Button>
            <Popover
                id="click-popover"
                aria-expanded={isOpen}
                anchorEl={anchorRef.current}
                onClose={closePopover}
                open={isOpen}
            >
                <Popover.Title>Start Mission</Popover.Title>
                <Popover.Content>
                    <StartMissionSelect robots={robots} />
                </Popover.Content>
            </Popover>
        </>
    )
}

export default StartMissionButton
