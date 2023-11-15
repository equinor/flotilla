import { Button, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { NoOngoingMissionsPlaceholder } from './NoMissionPlaceholder'
import { OngoingMissionCard } from './OngoingMissionCard'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { useMissionsContext } from 'components/Contexts/MissionListsContext'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { useEffect, useState } from 'react'
import { Mission } from 'models/Mission'

const StyledOngoingMissionView = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`
const OngoingMissionSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`

const ButtonStyle = styled.div`
    display: block;
`

const OngoingMissionHeader = styled.div`
    display: grid;
    grid-direction: column;
    gap: 0.5rem;
`

export function OngoingMissionView() {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions } = useMissionsContext()
    const { installationCode } = useInstallationContext()
    const [ongingMissionsToDisplay, setOngoingMissionsToDisplay] = useState<Mission[]>([])

    let navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/history`
        navigate(path)
    }

    useEffect(() => {
        setOngoingMissionsToDisplay(
            ongoingMissions.filter(
                (m) => m.installationCode?.toLocaleLowerCase() === installationCode.toLocaleLowerCase()
            )
        )
    }, [ongoingMissions, installationCode])

    const ongoingMissionCards = ongingMissionsToDisplay.map(function (mission, index) {
        return <OngoingMissionCard key={index} mission={mission} />
    })

    return (
        <StyledOngoingMissionView>
            <OngoingMissionHeader>
                <Typography variant="h1" color="resting">
                    {TranslateText('Ongoing Missions')}
                </Typography>
            </OngoingMissionHeader>
            <OngoingMissionSection>
                {ongingMissionsToDisplay.length > 0 && ongoingMissionCards}
                {ongingMissionsToDisplay.length === 0 && <NoOngoingMissionsPlaceholder />}
            </OngoingMissionSection>
            <ButtonStyle>
                <Button variant="outlined" onClick={routeChange}>
                    <Icon name={Icons.Historic} />
                    {TranslateText('History')}
                </Button>
            </ButtonStyle>
        </StyledOngoingMissionView>
    )
}
