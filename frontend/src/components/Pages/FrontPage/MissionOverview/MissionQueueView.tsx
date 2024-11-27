import styled from 'styled-components'
import { MissionQueueCard, PlaceholderMissionCard } from './MissionQueueCard'
import { BackendAPICaller } from 'api/ApiCaller'
import { Mission, placeholderMission } from 'models/Mission'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { Robot } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { useEffect } from 'react'

const StyledMissionView = styled.div`
    display: flex;
    padding: 16px;
    flex-direction: column;
    align-items: flex-end;
    gap: 8px;
    align-self: stretch;
    border-top: 1px solid ${tokens.colors.ui.background__medium.hex};
`

export const RobotMissionQueueView = ({ robot }: { robot: Robot }): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { missionQueue, ongoingMissions, loadingMissionSet, setLoadingMissionSet } = useMissionsContext()
    const { setAlert, setListAlert } = useAlertContext()

    const robotMissionQueue = missionQueue.filter((mission) => mission.robot.id === robot.id)
    const robotOngoingMissions = ongoingMissions.filter((mission) => mission.robot.id === robot.id)

    const onDeleteMission = (mission: Mission) =>
        BackendAPICaller.deleteMission(mission.id).catch((_) => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent translatedMessage={TranslateText('Failed to delete mission from queue')} />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent
                    translatedMessage={TranslateText('Failed to delete mission from queue')}
                />,
                AlertCategory.ERROR
            )
        })

    useEffect(() => {
        setLoadingMissionSet((currentLoadingNames) => {
            const updatedLoadingMissionNames = new Set(currentLoadingNames)
            robotMissionQueue.forEach((mission) => updatedLoadingMissionNames.delete(mission.name))
            robotOngoingMissions.forEach((mission) => updatedLoadingMissionNames.delete(mission.name))
            return updatedLoadingMissionNames
        })
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [missionQueue, ongoingMissions])

    const missionQueueDisplay = robotMissionQueue.map((mission, index) => (
        <MissionQueueCard key={index} order={index + 1} mission={mission} onDeleteMission={onDeleteMission} />
    ))

    const loadingQueueDisplay = (
        <PlaceholderMissionCard
            key={'placeholder'}
            order={robotMissionQueue.length + 1}
            mission={placeholderMission}
            onDeleteMission={() => {}}
        />
    )

    return (
        <>
            {(robotMissionQueue.length > 0 || loadingMissionSet.size > 0) && (
                <StyledMissionView id={FrontPageSectionId.MissionQueue}>
                    {missionQueueDisplay}
                    {loadingMissionSet.size > 0 && loadingQueueDisplay}
                </StyledMissionView>
            )}
        </>
    )
}
