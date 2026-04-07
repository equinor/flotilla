import { InstallationContext } from 'components/Contexts/InstallationContext'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { useContext } from 'react'
import { Icon, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useRef, useState } from 'react'
import { getInspectionDeadline } from 'utils/StringFormatting'
import styled from 'styled-components'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { CondensedMissionDefinition } from 'models/CondensedMissionDefinition'
import { Icons } from 'utils/icons'
import { StyledButton } from 'components/Styles/StyledComponents'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { phone_width } from 'utils/constants'
import { useBackendApi } from 'api/UseBackendApi'
import { AllInspectionsTable } from './InspectionPage/InspectionTable'
import { Placeholder } from './InspectionPage/InspectionUtilities'
import { ScheduleMissionDialog } from './FrontPage/MissionOverview/ScheduleMissionDialog/ScheduleMissionDialog'

const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: end;
    @media (max-width: ${phone_width}) {
        align-items: start;
    }
`
const StyledMissionButton = styled.div`
    display: flex;
    padding-bottom: 30px;
`
const StyledPlaceholderContent = styled.div`
    width: 70vw;
`
const StyledView = styled.div`
    display: flex;
    align-items: flex-start;
`
const AlignedTextButton = styled(StyledButton)`
    text-align: left;
`

export const PredefinedMissionsPage = () => {
    const { alerts, setAlert, setListAlert } = useAlertContext()
    const { installation } = useContext(InstallationContext)

    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [isFetchingMissions, setIsFetchingMissions] = useState<boolean>(false)
    const [isScheduleMissionDialogOpen, setIsScheduleMissionDialogOpen] = useState<boolean>(false)
    const [missions, setMissions] = useState<CondensedMissionDefinition[]>([])
    const backendApi = useBackendApi()

    const isScheduleButtonDisabled = enabledRobots.length === 0 || installation.installationCode === ''

    const anchorRef = useRef<HTMLButtonElement>(null)

    const allInspections = missionDefinitions.map((m) => {
        return {
            missionDefinition: m,
            deadline: m.lastSuccessfulRun
                ? getInspectionDeadline(m.inspectionFrequency, m.lastSuccessfulRun.endTime!)
                : undefined,
        }
    })

    const fetchMissions = () => {
        setIsFetchingMissions(true)
        backendApi
            .getAvailableMissions(installation.installationCode as string)
            .then((missions) => {
                setMissions(missions)
                setIsFetchingMissions(false)
            })
            .catch(() => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to retrieve missions')} />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent translatedMessage={TranslateText('Failed to retrieve missions')} />,
                    AlertCategory.ERROR
                )
                setIsFetchingMissions(false)
            })
    }

    const onClickScheduleMission = () => {
        setIsScheduleMissionDialogOpen(true)
        fetchMissions()
    }

    const AddPredefinedMissionsButton = () => (
        <AlignedTextButton onClick={onClickScheduleMission} disabled={isScheduleButtonDisabled} ref={anchorRef}>
            <Icon name={Icons.Add} size={16} />
            {TranslateText('Add predefined mission to queue')}
        </AlignedTextButton>
    )

    return (
        <>
            <Header alertDict={alerts} installation={installation} />
            <NavBar />
            <StyledView>
                <StyledContent>
                    <StyledMissionButton>
                        {isScheduleMissionDialogOpen && (
                            <ScheduleMissionDialog
                                isFetchingMissions={isFetchingMissions}
                                missions={missions}
                                onClose={() => setIsScheduleMissionDialogOpen(false)}
                            />
                        )}
                        <AddPredefinedMissionsButton />
                    </StyledMissionButton>
                    {allInspections.length > 0 ? (
                        <AllInspectionsTable inspections={allInspections} />
                    ) : (
                        <StyledPlaceholderContent>
                            <Placeholder>
                                <Typography variant="h4" color="disabled">
                                    {TranslateText('No predefined missions available')}
                                </Typography>
                            </Placeholder>
                        </StyledPlaceholderContent>
                    )}
                </StyledContent>
            </StyledView>
        </>
    )
}
