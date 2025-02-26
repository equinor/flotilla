import { Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { StyledTableBody, StyledTableCell } from 'components/Styles/StyledComponents'
import { DaysOfWeek } from 'models/AutoScheduleFrequency'
import { config } from 'config'
import { useNavigate } from 'react-router-dom'
import styled from 'styled-components'

const StyledSection = styled.div`
    display: flex;
    flex-direction: column;
    max-width: 960px;
    gap: 1rem;
`
const StyledTableRow = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
`
const StyledHeader = styled.div`
    gap: 0px;
`
const StyledDayOverview = styled.div`
    display: grid;
    gap: 0px;
`

const AutoScheduleList = () => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const navigate = useNavigate()

    const autoScheduleMissionDefinitions = missionDefinitions.filter((m) => m.autoScheduleFrequency)

    const allDays = [
        DaysOfWeek.Monday,
        DaysOfWeek.Tuesday,
        DaysOfWeek.Wednesday,
        DaysOfWeek.Thursday,
        DaysOfWeek.Friday,
        DaysOfWeek.Saturday,
        DaysOfWeek.Sunday,
    ]

    const DayOverview = () =>
        allDays.map((day) => {
            const missionDefinitions = autoScheduleMissionDefinitions.filter((m) =>
                m.autoScheduleFrequency!.daysOfWeek.includes(day)
            )
            const timeMissionPairs = missionDefinitions
                .map((mission) =>
                    mission.autoScheduleFrequency!.timesOfDay.map((time) => {
                        return { time, mission }
                    })
                )
                .flat()
                .sort((a, b) => (a.time > b.time ? 1 : -1))

            return (
                <Table key={day}>
                    <Table.Head>
                        <Table.Row>
                            <StyledTableCell>{TranslateText(day)}</StyledTableCell>
                        </Table.Row>
                    </Table.Head>
                    <StyledTableBody>
                        {timeMissionPairs.length > 0 ? (
                            timeMissionPairs.map(({ time, mission }) => (
                                <Table.Row
                                    key={mission.id + time}
                                    onClick={() =>
                                        navigate(`${config.FRONTEND_BASE_ROUTE}/mission-definition/${mission.id}`)
                                    }
                                >
                                    <Table.Cell>
                                        <StyledTableRow>
                                            <Typography>{`${time.substring(0, 5)}`}</Typography>
                                            <Typography link>{mission.name}</Typography>
                                        </StyledTableRow>
                                    </Table.Cell>
                                </Table.Row>
                            ))
                        ) : (
                            <Table.Row>
                                <Table.Cell>
                                    <Typography>{TranslateText('No missions')}</Typography>
                                </Table.Cell>
                            </Table.Row>
                        )}
                    </StyledTableBody>
                </Table>
            )
        })

    return (
        <>
            {autoScheduleMissionDefinitions.length > 0 && (
                <StyledSection>
                    <StyledHeader>
                        <Typography>
                            {TranslateText('These missions will be automatically scheduled at the specified time')}
                        </Typography>
                    </StyledHeader>
                    <StyledDayOverview>
                        <DayOverview />
                    </StyledDayOverview>
                </StyledSection>
            )}
        </>
    )
}

export const AutoScheduleSection = () => {
    return AutoScheduleList()
}
