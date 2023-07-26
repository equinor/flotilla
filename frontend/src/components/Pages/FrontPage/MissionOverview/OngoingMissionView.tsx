import { Button, Typography, Icon } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/Mission'
import { useCallback, useEffect, useState } from 'react'
import styled from 'styled-components'
import { RefreshProps } from '../FrontPage'
import { NoOngoingMissionsPlaceholder } from './NoMissionPlaceholder'
import { OngoingMissionCard } from './OngoingMissionCard'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { Icons } from 'utils/icons'
import { BackendAPICaller } from 'api/ApiCaller'

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

export function OngoingMissionView({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const missionPageSize = 100
    const [missions, setMissions] = useState<Mission[]>([])

    const updateOngoingAndPausedMissions = useCallback(() => {
        BackendAPICaller.getMissionRuns({
            statuses: [MissionStatus.Ongoing, MissionStatus.Paused],
            pageSize: missionPageSize,
            orderBy: 'StartTime desc',
        }).then((missions) => {
            setMissions(missions.content)
        })
    }, [])

    useEffect(() => {
        updateOngoingAndPausedMissions()
    }, [updateOngoingAndPausedMissions])

    useEffect(() => {
        const id = setInterval(() => {
            updateOngoingAndPausedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval, updateOngoingAndPausedMissions])

    var missionDisplay = missions.map((mission, index) => <OngoingMissionCard key={index} mission={mission} />)

    let navigate = useNavigate()
    const routeChange = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/history`
        navigate(path)
    }

    return (
        <StyledOngoingMissionView>
            <Typography variant="h1" color="resting">
                {TranslateText('Ongoing Missions')}
            </Typography>
            <OngoingMissionSection>
                {missions.length > 0 && missionDisplay}
                {missions.length === 0 && <NoOngoingMissionsPlaceholder />}
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
