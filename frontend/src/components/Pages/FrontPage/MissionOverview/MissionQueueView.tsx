import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionQueueCard } from './MissionQueueCard'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission } from 'models/Mission'
import { EmptyMissionQueuePlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'
import { Robot } from 'models/Robot'
import { RefreshProps } from '../FrontPage'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { CreateMissionButton } from './CreateMissionButton'
import { MissionDefinition } from 'models/MissionDefinition'
import { useMissionQueueContext } from 'components/Contexts/MissionQueueContext'

const StyledMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`

const MissionTable = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 1rem;
`

const MissionButtonView = styled.div`
    display: flex;
    gap: 1rem;
`
const mapEchoMissionToString = (missions: MissionDefinition[]): Map<string, MissionDefinition> => {
    var missionMap = new Map<string, MissionDefinition>()
    missions.forEach((mission: MissionDefinition) => {
        missionMap.set(mission.echoMissionId + ': ' + mission.name, mission)
    })
    return missionMap
}

export function MissionQueueView({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const { missionQueue } = useMissionQueueContext()

    const missionPageSize = 100
    const [missionMap, setMissionMap] = useState<{ [id: string] : Mission }>({})
    const [missionOrder, setMissionOrder] = useState<string[]>(Object.keys(missionMap))

    const [selectedEchoMissions, setSelectedEchoMissions] = useState<MissionDefinition[]>([])
    const [selectedRobot, setSelectedRobot] = useState<Robot>()
    const [echoMissions, setEchoMissions] = useState<Map<string, MissionDefinition>>(
        new Map<string, MissionDefinition>()
    )
    const [robotOptions, setRobotOptions] = useState<Robot[]>([])
    const [scheduleButtonDisabled, setScheduleButtonDisabled] = useState<boolean>(true)
    const [frontPageScheduleButtonDisabled, setFrontPageScheduleButtonDisabled] = useState<boolean>(true)
    const [isFetchingEchoMissions, setIsFetchingEchoMissions] = useState<boolean>(false)
    const { installationCode } = useInstallationContext()

    const fetchEchoMissions = () => {
        setIsFetchingEchoMissions(true)
        BackendAPICaller.getAvailableEchoMission(installationCode as string).then((missions) => {
            const echoMissionsMap: Map<string, MissionDefinition> = mapEchoMissionToString(missions)
            setEchoMissions(echoMissionsMap)
            setIsFetchingEchoMissions(false)
        })
    }

    const onChangeMissionSelections = (selectedEchoMissions: string[]) => {
        var echoMissionsToSchedule: MissionDefinition[] = []
        if (echoMissions) {
            selectedEchoMissions.forEach((selectedEchoMission: string) => {
                echoMissionsToSchedule.push(echoMissions.get(selectedEchoMission) as MissionDefinition)
            })
        }
        setSelectedEchoMissions(echoMissionsToSchedule)
    }
    const onSelectedRobot = (selectedRobot: Robot) => {
        if (robotOptions === undefined) return

        setSelectedRobot(selectedRobot)
    }

    const onScheduleButtonPress = () => {
        if (selectedRobot === undefined) return

        selectedEchoMissions.forEach((mission: MissionDefinition) => {
            BackendAPICaller.postMission(mission.echoMissionId, selectedRobot.id, installationCode)
        })

        setSelectedEchoMissions([])
        setSelectedRobot(undefined)
    }

    const onDeleteMission = (mission: Mission) => {
        BackendAPICaller.deleteMission(mission.id) //.catch((e) => handleError(e))
        let localMissionOrder = missionOrder
        const index = localMissionOrder.indexOf(mission.id, 0);
        if (index > -1) {
            missionOrder.splice(index, 1);
        }
        setMissionOrder(localMissionOrder)
    }

    const onReorderMission = (mission1: Mission, mission2: Mission) => {
        let localOrdering = missionOrder
        let missionIndex: number = localOrdering.findIndex(id => id === mission.id)
        if (missionIndex + offset >= localOrdering.length || missionIndex + offset < 0)
            return
        var element = localOrdering[missionIndex];
        localOrdering.splice(missionIndex, 1);
        localOrdering.splice(missionIndex + offset, 0, element);
        setMissionOrder(localOrdering)
        // TODO: rely on the ordering we get from the API and accept that
        // it's slow. Instead send requests of swapping start time between
        // two missions. This way we ensure consistency between frontend/backend.
        // We can more easily detect failure this way. Document this in PR, that
        // we first tried to send the total ordering within one requested page, and
        // update frontend first, but that the consistency issues became too much
        // so that to ensure consistency and to easily detect failure in the frontend
        // we instead just rely on backend info for the true ordering. Mention
        // that we originally had a map of missions and a list of ID orderings. Now
        // we will instead go back to using one list of missions
        BackendAPICaller.updateMissionOrder(
            {
                status: MissionStatus.Pending,
                pageSize: missionPageSize
            }, 
            localOrdering)
    }

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getEnabledRobots().then((robots) => {
                setRobotOptions(robots)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval])

    useEffect(() => {
        const id = setInterval(() => {
            BackendAPICaller.getMissionRuns({
                status: MissionStatus.Pending,
                pageSize: missionPageSize,
                orderBy: 'DesiredStartTime',
            }).then((missions) => {
                let localMissionMap: {[id: string] : Mission} = {}
                missions.content.forEach(mission => {
                    localMissionMap[mission.id] = mission
                })
                setMissionMap(localMissionMap)

                let difference = missions.content.map(m => m.id).filter(x => !missionOrder.includes(x));
                missionOrder.push(...difference)
                setMissionOrder(missionOrder)
            })
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval])

    useEffect(() => {
        if (selectedRobot === undefined || selectedEchoMissions.length === 0) {
            setScheduleButtonDisabled(true)
        } else {
            setScheduleButtonDisabled(false)
        }
    }, [selectedRobot, selectedEchoMissions])

    useEffect(() => {
        if (robotOptions.length === 0 || installationCode === '') {
            setFrontPageScheduleButtonDisabled(true)
        } else {
            setFrontPageScheduleButtonDisabled(false)
        }
    }, [robotOptions, installationCode])

    var missionQueueDisplay = missionOrder.map((id, index) => {
        if (!id || !missionMap[id])
            return
        return <MissionQueueCard key={id} order={index} mission={missionMap[id]} onDeleteMission={onDeleteMission} onReorderMission={onReorderMission} />
    })

    return (
        <StyledMissionView>
            <Typography variant="h1" color="resting">
                {TranslateText('Mission Queue')}
            </Typography>
            <MissionTable>
                {missionOrder.length > 0 && missionQueueDisplay}
                {missionOrder.length === 0 && <EmptyMissionQueuePlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <ScheduleMissionDialog
                    robotOptions={robotOptions}
                    echoMissionsOptions={Array.from(echoMissions.keys())}
                    onChangeMissionSelections={onChangeMissionSelections}
                    selectedMissions={selectedEchoMissions}
                    onSelectedRobot={onSelectedRobot}
                    onScheduleButtonPress={onScheduleButtonPress}
                    fetchEchoMissions={fetchEchoMissions}
                    scheduleButtonDisabled={scheduleButtonDisabled}
                    frontPageScheduleButtonDisabled={frontPageScheduleButtonDisabled}
                    isFetchingEchoMissions={isFetchingEchoMissions}
                ></ScheduleMissionDialog>
                {CreateMissionButton()}
            </MissionButtonView>
        </StyledMissionView>
    )
}
