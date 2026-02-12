import styled from 'styled-components'
import { MissionQueueCard, PlaceholderMissionCard } from './MissionQueueCard'
import { Mission, placeholderMission } from 'models/Mission'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { RobotWithoutTelemetry } from 'models/Robot'
import { tokens } from '@equinor/eds-tokens'
import { useEffect, useState } from 'react'
import { Button, Dialog, Typography } from '@equinor/eds-core-react'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { useBackendApi } from 'api/UseBackendApi'

const StyledMissionView = styled.div`
    display: flex;
    padding: 16px;
    flex-direction: column;
    gap: 8px;
    align-self: stretch;
    border-top: 1px solid ${tokens.colors.ui.background__medium.hex};
`
const StyledTitleAndButton = styled.div`
    display: flex;
    flex-directon: row;
    justify-content: space-between;
    align-items: center;
`

const StyledWrapper = styled.div`
    display: flex;
    gap: 8px;
`

export const RobotMissionQueueView = ({ robot }: { robot: RobotWithoutTelemetry }) => {
    const { TranslateText } = useLanguageContext()
    const { missionQueue, ongoingMissions, loadingRobotMissionSet, setLoadingRobotMissionSet } = useMissionsContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [isdialogOpen, setIsDialogOpen] = useState(false)

    const robotMissionQueue = missionQueue.filter((mission) => mission.robot.id === robot.id)
    const robotOngoingMissions = ongoingMissions.filter((mission) => mission.robot.id === robot.id)
    const robotLoadingMissions = Array.from(loadingRobotMissionSet).filter((robotMissionName) =>
        robotMissionName.includes(robot.id)
    )
    const backendApi = useBackendApi()

    const onDeleteMission = (mission: Mission) =>
        backendApi.deleteMission(mission.id).catch(() => {
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

    const onDeleteAllMissions = () =>
        backendApi.deleteAllMissions().catch(() => {
            setAlert(
                AlertType.RequestFail,
                <FailedRequestAlertContent
                    translatedMessage={TranslateText('Failed to delete all missions from queue')}
                />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.RequestFail,
                <FailedRequestAlertListContent
                    translatedMessage={TranslateText('Failed to delete all missions from queue')}
                />,
                AlertCategory.ERROR
            )
        })

    const handleButton = () => {
        onDeleteAllMissions()
        setIsDialogOpen(false)
    }

    useEffect(() => {
        setLoadingRobotMissionSet((currentLoadingNames) => {
            const updatedLoadingMissionNames = new Set(currentLoadingNames)
            robotMissionQueue.forEach((mission) => updatedLoadingMissionNames.delete(mission.name + robot.id))
            robotOngoingMissions.forEach((mission) => updatedLoadingMissionNames.delete(mission.name + robot.id))
            return updatedLoadingMissionNames
        })
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
            {(robotMissionQueue.length > 0 || robotLoadingMissions.length > 0) && (
                <StyledMissionView>
                    <StyledTitleAndButton>
                        <Typography variant="h5">{TranslateText('Queued Missions')}</Typography>
                        <Button variant="ghost" aria-haspopup="dialog" onClick={() => setIsDialogOpen(true)}>
                            <Typography variant="caption">{TranslateText('Remove all')}</Typography>
                        </Button>
                        <StyledDialog open={isdialogOpen} onClose={() => setIsDialogOpen(false)} isDismissable={true}>
                            <Dialog.Header>
                                <Dialog.Title>
                                    <Typography variant="h3">{TranslateText('Remove all missions')}</Typography>
                                </Dialog.Title>
                            </Dialog.Header>
                            <Dialog.CustomContent>
                                <Typography variant="body_short">
                                    {TranslateText('Remove all missions dialog text')}
                                </Typography>
                            </Dialog.CustomContent>
                            <Dialog.Actions>
                                <StyledWrapper>
                                    <Button variant="outlined" onClick={() => setIsDialogOpen(false)}>
                                        {TranslateText('Cancel')}
                                    </Button>
                                    <Button onClick={handleButton}>{TranslateText('Remove all missions')}</Button>
                                </StyledWrapper>
                            </Dialog.Actions>
                        </StyledDialog>
                    </StyledTitleAndButton>
                    {missionQueueDisplay}
                    {robotLoadingMissions.length > 0 && loadingQueueDisplay}
                </StyledMissionView>
            )}
        </>
    )
}
