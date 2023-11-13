import { Autocomplete, Button, Dialog, Typography, Popover, Icon, CircularProgress } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useRef, useState, useEffect } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { MissionButton } from './MissionButton'
import { Robot } from 'models/Robot'
import { EchoMissionDefinition } from 'models/MissionDefinition'
import { StyledAutoComplete, StyledDialog } from 'components/Styles/StyledComponents'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'

interface IProps {
    setLoadingMissionSet: (foo: (missionIds: Set<string>) => Set<string>) => void
}

const StyledMissionDialog = styled.div`
    display: flex;
    justify-content: space-between;
`

const StyledMissionSection = styled.div`
    display: flex;
    margin-left: auto;
    margin-right: 0;
    gap: 10px;
`
const StyledLoading = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 3rem;
    gap: 1rem;
`

const mapEchoMissionToString = (missions: EchoMissionDefinition[]): Map<string, EchoMissionDefinition> => {
    var missionMap = new Map<string, EchoMissionDefinition>()
    missions.forEach((mission: EchoMissionDefinition) => {
        missionMap.set(mission.echoMissionId + ': ' + mission.name, mission)
    })
    return missionMap
}

export const ScheduleMissionDialog = (props: IProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const { enabledRobots } = useRobotContext()
    const [isScheduleMissionDialogOpen, setIsScheduleMissionDialogOpen] = useState<boolean>(false)
    const [isEmptyEchoMissionsDialogOpen, setIsEmptyEchoMissionsDialogOpen] = useState<boolean>(false)
    const [isPopoverOpen, setIsPopoverOpen] = useState<boolean>(false)
    const [isScheduleMissionsPressed, setIsScheduleMissionsPressed] = useState<boolean>(false)
    const [selectedEchoMissions, setSelectedEchoMissions] = useState<EchoMissionDefinition[]>([])
    const [scheduleButtonDisabled, setScheduleButtonDisabled] = useState<boolean>(true)
    const { setAlert } = useAlertContext()
    const [isFetchingEchoMissions, setIsFetchingEchoMissions] = useState<boolean>(false)
    const [frontPageScheduleButtonDisabled, setFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [echoMissions, setEchoMissions] = useState<Map<string, EchoMissionDefinition>>(
        new Map<string, EchoMissionDefinition>()
    )
    const anchorRef = useRef<HTMLButtonElement>(null)

    const echoMissionsOptions = Array.from(echoMissions.keys())

    useEffect(() => {
        if (enabledRobots.length === 0 || installationCode === '') {
            setFrontPageScheduleButtonDisabled(true)
        } else {
            setFrontPageScheduleButtonDisabled(false)
        }
    }, [enabledRobots, installationCode])

    const fetchEchoMissions = () => {
        setIsFetchingEchoMissions(true)
        BackendAPICaller.getAvailableEchoMissions(installationCode as string)
            .then((missions) => {
                const echoMissionsMap: Map<string, EchoMissionDefinition> = mapEchoMissionToString(missions)
                setEchoMissions(echoMissionsMap)
                setIsFetchingEchoMissions(false)
            })
            .catch((_) => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent message={'Failed to retrieve echo missions'} />
                )
                setIsFetchingEchoMissions(false)
            })
    }

    useEffect(() => {
        if (!selectedRobot || selectedEchoMissions.length === 0) {
            setScheduleButtonDisabled(true)
        } else {
            setScheduleButtonDisabled(false)
        }
    }, [selectedRobot, selectedEchoMissions])

    useEffect(() => {
        if (!isFetchingEchoMissions && isScheduleMissionsPressed) {
            if (echoMissionsOptions.length === 0) setIsEmptyEchoMissionsDialogOpen(true)
            else setIsScheduleMissionDialogOpen(true)
            setIsScheduleMissionsPressed(false)
        }
    }, [isScheduleMissionsPressed, echoMissions, isFetchingEchoMissions])

    let timer: ReturnType<typeof setTimeout>
    const openPopover = () => {
        if (frontPageScheduleButtonDisabled) setIsPopoverOpen(true)
    }

    const onChangeMissionSelections = (selectedEchoMissions: string[]) => {
        var echoMissionsToSchedule: EchoMissionDefinition[] = []
        if (echoMissions) {
            selectedEchoMissions.forEach((selectedEchoMission: string) => {
                echoMissionsToSchedule.push(echoMissions.get(selectedEchoMission) as EchoMissionDefinition)
            })
        }
        setSelectedEchoMissions(echoMissionsToSchedule)
    }

    const onSelectedRobot = (selectedRobot: Robot) => {
        if (!enabledRobots) return
        setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = () => {
        if (!selectedRobot) return

        selectedEchoMissions.forEach((mission: EchoMissionDefinition) => {
            BackendAPICaller.postMission(mission.echoMissionId, selectedRobot.id, installationCode)
            props.setLoadingMissionSet((currentSet: Set<string>) => {
                const updatedSet: Set<string> = new Set(currentSet)
                updatedSet.add(String(mission.name))
                return updatedSet
            })
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    const closePopover = () => setIsPopoverOpen(false)

    const handleHover = () => {
        timer = setTimeout(() => {
            openPopover()
        }, 300)
    }

    const handleClose = () => {
        clearTimeout(timer)
        closePopover()
    }

    const onClickScheduleMission = () => {
        setIsScheduleMissionsPressed(true)
        fetchEchoMissions()
    }

    // TODO: divide up each dialog into a different object

    return (
        <>
            <div
                onPointerDown={handleHover}
                onPointerEnter={handleHover}
                onPointerLeave={handleClose}
                onFocus={openPopover}
                onBlur={handleClose}
            >
                <Button
                    onClick={() => {
                        onClickScheduleMission()
                    }}
                    disabled={frontPageScheduleButtonDisabled}
                    ref={anchorRef}
                >
                    <>
                        <Icon name={Icons.Add} size={16} />
                        {TranslateText('Add mission')}
                    </>
                </Button>
            </div>

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
                <StyledDialog open={isFetchingEchoMissions && isScheduleMissionsPressed} isDismissable>
                    <StyledAutoComplete>
                        <StyledLoading>
                            <CircularProgress />
                            <Typography>{TranslateText('Fetching missions from Echo') + '...'}</Typography>
                        </StyledLoading>
                        <StyledMissionSection>
                            <Button
                                onClick={() => {
                                    setIsScheduleMissionsPressed(false)
                                }}
                                variant="outlined"
                            >
                                {TranslateText('Cancel')}
                            </Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </StyledDialog>
            </StyledMissionDialog>

            <StyledMissionDialog>
                <Dialog open={isEmptyEchoMissionsDialogOpen} isDismissable>
                    <StyledAutoComplete>
                        <Typography variant="h5">
                            {TranslateText('This installation has no missions - Please create mission')}
                        </Typography>
                        <StyledMissionSection>
                            <MissionButton />
                            <Button
                                onClick={() => {
                                    setIsEmptyEchoMissionsDialogOpen(false)
                                }}
                                variant="outlined"
                            >
                                {TranslateText('Cancel')}
                            </Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </Dialog>
            </StyledMissionDialog>

            <StyledMissionDialog>
                <StyledDialog open={isScheduleMissionDialogOpen} isDismissable>
                    <StyledAutoComplete>
                        <Typography variant="h3">{TranslateText('Add mission')}</Typography>
                        <Autocomplete
                            options={echoMissionsOptions}
                            onOptionsChange={(changes) => onChangeMissionSelections(changes.selectedItems)}
                            label={TranslateText('Select missions')}
                            multiple
                            placeholder={`${selectedEchoMissions.length}/${
                                Array.from(echoMissionsOptions.keys()).length
                            } ${TranslateText('selected')}`}
                            autoWidth={true}
                            onFocus={(e) => e.preventDefault()}
                        />
                        <Autocomplete
                            optionLabel={(r) => r.name + ' (' + r.model.type + ')'}
                            options={enabledRobots.filter(
                                (r) =>
                                    r.currentInstallation.toLocaleLowerCase() === installationCode.toLocaleLowerCase()
                            )}
                            label={TranslateText('Select robot')}
                            onOptionsChange={(changes) => onSelectedRobot(changes.selectedItems[0])}
                            autoWidth={true}
                            onFocus={(e) => e.preventDefault()}
                        />
                        <StyledMissionSection>
                            <Button
                                onClick={() => {
                                    setIsScheduleMissionDialogOpen(false)
                                }}
                                variant="outlined"
                            >
                                {TranslateText('Cancel')}
                            </Button>
                            <Button
                                onClick={() => {
                                    onScheduleButtonPress()
                                    setIsScheduleMissionDialogOpen(false)
                                }}
                                disabled={scheduleButtonDisabled}
                            >
                                {TranslateText('Add mission')}
                            </Button>
                        </StyledMissionSection>
                    </StyledAutoComplete>
                </StyledDialog>
            </StyledMissionDialog>
        </>
    )
}
