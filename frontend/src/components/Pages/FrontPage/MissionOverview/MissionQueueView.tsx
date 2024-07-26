import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { MissionQueueCard } from './MissionQueueCard'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect } from 'react'
import { Mission, placeholderMission } from 'models/Mission'
import { EmptyMissionQueuePlaceholder } from './NoMissionPlaceholder'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent } from 'components/Alerts/FailedRequestAlert'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { AlertCategory } from 'components/Alerts/AlertsBanner'

const StyledMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    align-content: start;
    gap: 1rem;
`

const MissionTable = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    align-items: center;
    gap: 1rem;
`

export const MissionQueueView = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { missionQueue, ongoingMissions, loadingMissionSet, setLoadingMissionSet } = useMissionsContext()
    const { setAlert } = useAlertContext()

    const onDeleteMission = (mission: Mission) =>
        BackendAPICaller.deleteMission(mission.id).catch((_) =>
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent translatedMessage={TranslateText('Failed to delete mission from queue')} />,
                AlertCategory.ERROR
            )
        )

    useEffect(() => {
        setLoadingMissionSet((currentLoadingNames) => {
            const updatedLoadingMissionNames = new Set(currentLoadingNames)
            missionQueue.forEach((mission) => updatedLoadingMissionNames.delete(mission.name))
            ongoingMissions.forEach((mission) => updatedLoadingMissionNames.delete(mission.name))
            return updatedLoadingMissionNames
        })
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [missionQueue, ongoingMissions])

    const missionQueueDisplay = missionQueue.map((mission, index) => (
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
        <StyledMissionView id={FrontPageSectionId.MissionQueue}>
            <Typography variant="h1" color="resting">
                {TranslateText('Mission Queue')}
            </Typography>
            <MissionTable>
                {missionQueue.length > 0 && missionQueueDisplay}
                {loadingMissionSet.size > 0 && loadingQueueDisplay}
                {loadingMissionSet.size === 0 && missionQueue.length === 0 && <EmptyMissionQueuePlaceholder />}
            </MissionTable>
        </StyledMissionView>
    )
}
