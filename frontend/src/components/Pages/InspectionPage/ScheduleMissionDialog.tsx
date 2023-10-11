import { Autocomplete, Button, Card, Dialog, Typography, Popover } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useRef, useState, useEffect } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Robot } from 'models/Robot'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { BackendAPICaller } from 'api/ApiCaller'

interface IProps {
    missions: CondensedMissionDefinition[]
    refreshInterval: number
    closeDialog: () => void
}

const StyledMissionDialog = styled.div`
    display: flex;
    justify-content: space-between;
`
const StyledAutoComplete = styled(Card)`
    display: flex;
    justify-content: center;
    padding: 8px;
    gap: 25px;
`
const StyledMissionSection = styled.div`
    display: flex;
    margin-left: auto;
    margin-right: 0;
    gap: 10px;
`
const StyledDialog = styled(Dialog)`
    display: flex;
    padding: 1rem;
    width: 320px;
`

export const ScheduleMissionDialog = (props: IProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [isPopoverOpen, setIsPopoverOpen] = useState<boolean>(false)
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [robotOptions, setRobotOptions] = useState<Robot[]>([])
    const anchorRef = useRef<HTMLButtonElement>(null)

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getEnabledRobots()
                .then((robots) =>
                    robots.filter(
                        (robots) => robots.currentInstallation.toLowerCase() === installationCode.toLowerCase()
                    )
                )
                .then((robots) => {
                    setRobotOptions(robots)
                })
        }, props.refreshInterval)
        return () => clearInterval(id)
    }, [props.refreshInterval])

    let timer: ReturnType<typeof setTimeout>

    const onSelectedRobot = (selectedRobot: Robot) => {
        if (robotOptions === undefined) return

        setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = () => {
        if (selectedRobot === undefined) return

        props.missions.forEach((mission) => BackendAPICaller.scheduleMissionDefinition(mission.id, selectedRobot.id))

        setSelectedRobot(undefined)
    }

    const closePopover = () => setIsPopoverOpen(false)

    const handleClose = () => {
        clearTimeout(timer)
        closePopover()
    }

    return (
        <>
            <Popover
                anchorEl={anchorRef.current}
                onClose={handleClose}
                open={isPopoverOpen && installationCode === ''}
                placement="top"
            >
                <Popover.Content>
                    <Typography variant="body_short">{TranslateText('Please select installation')}</Typography>
                </Popover.Content>
            </Popover>

            <StyledMissionDialog>
                <StyledDialog open={true}>
                    <StyledAutoComplete>
                        <Autocomplete
                            optionLabel={(r) => r.name + ' (' + r.model.type + ')'}
                            options={robotOptions}
                            label={TranslateText('Select robot')}
                            onOptionsChange={(changes) => onSelectedRobot(changes.selectedItems[0])}
                            autoWidth={true}
                            onFocus={(e) => e.preventDefault()}
                        />
                        <StyledMissionSection>
                            <Button
                                onClick={() => {
                                    props.closeDialog()
                                }}
                                variant="outlined"
                                color="primary"
                            >
                                {' '}
                                {TranslateText('Cancel')}{' '}
                            </Button>
                            <Button
                                onClick={() => {
                                    onScheduleButtonPress()
                                    props.closeDialog()
                                }}
                                disabled={!selectedRobot}
                            >
                                {' '}
                                {TranslateText('Queue mission')}
                            </Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </StyledDialog>
            </StyledMissionDialog>
        </>
    )
}
