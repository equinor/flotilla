import { Button, Typography, Popover, Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useRef, useState, useEffect } from 'react'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { EchoMissionDefinition } from 'models/MissionDefinition'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { BackendAPICaller } from 'api/ApiCaller'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'
import { FetchingMissionsDialog } from './FetchingMissionsDialog'
import { NoMissionsDialog } from './NoMissionsDialog'
import { SelectMissionsToScheduleDialog } from './SelectMissionsToScheduleDialog'
import { useMissionsContext } from 'components/Contexts/MissionListsContext'

const mapEchoMissionToString = (missions: EchoMissionDefinition[]): Map<string, EchoMissionDefinition> => {
    var missionMap = new Map<string, EchoMissionDefinition>()
    missions.forEach((mission: EchoMissionDefinition) => {
        missionMap.set(mission.echoMissionId + ': ' + mission.name, mission)
    })
    return missionMap
}

export const ScheduleMissionDialog = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const { enabledRobots } = useRobotContext()
    const { setAlert } = useAlertContext()
    const [isScheduleMissionDialogOpen, setIsScheduleMissionDialogOpen] = useState<boolean>(false)
    const [isEmptyEchoMissionsDialogOpen, setIsEmptyEchoMissionsDialogOpen] = useState<boolean>(false)
    const [isPopoverOpen, setIsPopoverOpen] = useState<boolean>(false)
    const [isScheduleMissionsPressed, setIsScheduleMissionsPressed] = useState<boolean>(false)
    const [isFetchingEchoMissions, setIsFetchingEchoMissions] = useState<boolean>(false)
    const [isFrontPageScheduleButtonDisabled, setIsFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const [echoMissions, setEchoMissions] = useState<Map<string, EchoMissionDefinition>>(
        new Map<string, EchoMissionDefinition>()
    )
    const { setLoadingMissionSet } = useMissionsContext()

    const anchorRef = useRef<HTMLButtonElement>(null)

    const echoMissionsOptions = Array.from(echoMissions.keys())

    useEffect(() => {
        if (enabledRobots.length === 0 || installationCode === '') {
            setIsFrontPageScheduleButtonDisabled(true)
        } else {
            setIsFrontPageScheduleButtonDisabled(false)
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
        if (!isFetchingEchoMissions && isScheduleMissionsPressed) {
            if (echoMissionsOptions.length === 0) setIsEmptyEchoMissionsDialogOpen(true)
            else setIsScheduleMissionDialogOpen(true)
            setIsScheduleMissionsPressed(false)
        }
    }, [isScheduleMissionsPressed, echoMissions, isFetchingEchoMissions, echoMissionsOptions.length])

    let timer: ReturnType<typeof setTimeout>
    const openPopover = () => {
        if (isFrontPageScheduleButtonDisabled) setIsPopoverOpen(true)
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
                    disabled={isFrontPageScheduleButtonDisabled}
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

            {isFetchingEchoMissions && isScheduleMissionsPressed && (
                <FetchingMissionsDialog closeDialog={() => setIsScheduleMissionsPressed(false)} />
            )}

            {isEmptyEchoMissionsDialogOpen && (
                <NoMissionsDialog closeDialog={() => setIsEmptyEchoMissionsDialogOpen(false)} />
            )}

            {isScheduleMissionDialogOpen && (
                <SelectMissionsToScheduleDialog
                    echoMissions={echoMissions}
                    closeDialog={() => setIsScheduleMissionDialogOpen(false)}
                    setLoadingMissionSet={setLoadingMissionSet}
                />
            )}
        </>
    )
}
