import { Button, Typography, Icon } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { NoOngoingMissionsPlaceholder } from './NoMissionPlaceholder'
import { OngoingMissionCard } from './OngoingMissionCard'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
import { tokens } from '@equinor/eds-tokens'

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
const StyledButton = styled(Button)`
    :hover {
        background-color: ${tokens.colors.interactive.primary__hover.hex};
    }
    background-color: ${tokens.colors.ui.background__default.hex};
`

export const OngoingMissionView = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions } = useMissionsContext()

    let navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/history`
        navigate(path)
    }

    const ongoingMissionCards = ongoingMissions.map((mission, index) => (
        <OngoingMissionCard key={index} mission={mission} />
    ))

    return (
        <StyledOngoingMissionView>
            <OngoingMissionHeader>
                <Typography variant="h1" color="resting">
                    {TranslateText('Ongoing Missions')}
                </Typography>
            </OngoingMissionHeader>
            <OngoingMissionSection>
                {ongoingMissions.length > 0 && ongoingMissionCards}
                {ongoingMissions.length === 0 && <NoOngoingMissionsPlaceholder />}
            </OngoingMissionSection>
            <ButtonStyle>
                <StyledButton variant="outlined" onClick={routeChange}>
                    <Icon name={Icons.Historic} />
                    {TranslateText('History')}
                </StyledButton>
            </ButtonStyle>
        </StyledOngoingMissionView>
    )
}
