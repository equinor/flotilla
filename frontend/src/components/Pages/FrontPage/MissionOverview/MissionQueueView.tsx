import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionQueueCard } from './MissionQueueCard'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { Mission, placeholderMission } from 'models/Mission'
import { EmptyMissionQueuePlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog/ScheduleMissionDialog'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionButton } from './MissionButton'
import { useMissionsContext } from 'components/Contexts/MissionListsContext'
import { useInstallationContext } from 'components/Contexts/InstallationContext'

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

export function MissionQueueView() {
    const { TranslateText } = useLanguageContext()
    const { missionQueue, ongoingMissions } = useMissionsContext()
    const [loadingMissionSet, setLoadingMissionSet] = useState<Set<string>>(new Set())
    const { installationCode } = useInstallationContext()

    const onDeleteMission = (mission: Mission) => BackendAPICaller.deleteMission(mission.id)

    const localMissionQueue = missionQueue.filter(
        (m) => m.installationCode?.toLocaleLowerCase() === installationCode.toLocaleLowerCase()
    )

    useEffect(() => {
        setLoadingMissionSet((currentLoadingNames) => {
            const updatedLoadingMissionNames = new Set(currentLoadingNames)
            missionQueue.forEach((mission) => updatedLoadingMissionNames.delete(mission.name))
            ongoingMissions.forEach((mission) => updatedLoadingMissionNames.delete(mission.name))
            return updatedLoadingMissionNames
        })
    }, [missionQueue, ongoingMissions])

    const missionQueueDisplay = localMissionQueue.map((mission, index) => (
        <MissionQueueCard key={index} order={index + 1} mission={mission} onDeleteMission={onDeleteMission} />
    ))

    const loadingQueueDisplay = (
        <MissionQueueCard
            key={'placeholder'}
            order={missionQueue.length + 1}
            mission={placeholderMission}
            onDeleteMission={() => {}}
        />
    )

    return (
        <StyledMissionView>
            <Typography variant="h1" color="resting">
                {TranslateText('Mission Queue')}
            </Typography>
            <MissionTable>
                {localMissionQueue.length > 0 && missionQueueDisplay}
                {loadingMissionSet.size > 0 && loadingQueueDisplay}
                {loadingMissionSet.size === 0 && localMissionQueue.length === 0 && <EmptyMissionQueuePlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <ScheduleMissionDialog setLoadingMissionSet={setLoadingMissionSet}></ScheduleMissionDialog>
                <MissionButton />
            </MissionButtonView>
        </StyledMissionView>
    )
}
