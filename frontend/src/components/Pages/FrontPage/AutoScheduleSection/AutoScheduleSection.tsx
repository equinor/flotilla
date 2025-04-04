import { Button, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { StyledDialog, StyledTableBody, StyledTableCell } from 'components/Styles/StyledComponents'
import { DaysOfWeek } from 'models/AutoScheduleFrequency'
import { config } from 'config'
import { useNavigate } from 'react-router-dom'
import styled from 'styled-components'
import { capitalizeFirstLetter } from 'utils/StringFormatting'
import { StyledIcon } from 'components/Pages/InspectionPage/InspectionTable'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { FormCard } from 'components/Pages/MissionDefinitionPage/MissionDefinitionStyledComponents'
import { MissionDefinitionEditDialogContent } from 'components/Pages/MissionDefinitionPage/MissionDefinitionPage'
import { MissionDefinition } from 'models/MissionDefinition'
import { SelectMissionsComponent } from '../MissionOverview/ScheduleMissionDialog/SelectMissionsToScheduleDialog'

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
const StyledButtonSection = styled.div`
    display: flex;
    justify-content: flex-end;
    align-items: flex-start;
    gap: 8px;
    align-self: stretch;

    @media (max-width: 600px) {
        justify-content: flex-start;
    }
`
const StyledButton = styled(Button)`
    display: flex;
    padding: 0px 16px;
    justify-content: center;
    align-items: center;
    gap: 8px;
`

const StyledFormCard = styled(FormCard)`
    margin-top: 2px;
`

const AutoScheduleList = () => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [dialogOpen, setDialogOpen] = useState<boolean>(false)
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>([])
    const navigate = useNavigate()

    const autoScheduleMissionDefinitions = missionDefinitions.filter((m) => m.autoScheduleFrequency)

    const openDialog = () => {
        setDialogOpen(true)
    }
    const closeDialog = () => {
        setDialogOpen(false)
        setSelectedMissions([])
    }

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
                    mission.autoScheduleFrequency!.timesOfDayCET.map((time) => {
                        return { time, mission }
                    })
                )
                .flat()
                .sort((a, b) => (a.time > b.time ? 1 : -1))

            return (
                <Table key={day}>
                    <Table.Head>
                        <Table.Row>
                            <StyledTableCell>{capitalizeFirstLetter(TranslateText(day))}</StyledTableCell>
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
            <StyledButtonSection>
                <StyledButton onClick={openDialog}>
                    <StyledIcon name={Icons.Add} size={24} />
                    {TranslateText('New scheduled mission')}
                </StyledButton>
            </StyledButtonSection>
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
                    {dialogOpen && (
                        <StyledDialog open={true}>
                            <StyledDialog.Header>
                                <StyledDialog.Title>
                                    <Typography variant="h3">{TranslateText('New scheduled mission')}</Typography>
                                </StyledDialog.Title>
                            </StyledDialog.Header>
                            <StyledDialog.CustomContent>
                                <SelectMissionsComponent
                                    missions={missionDefinitions}
                                    selectedMissions={selectedMissions}
                                    setSelectedMissions={setSelectedMissions}
                                    multiple={false}
                                />
                                {dialogOpen && selectedMissions.length === 1 && (
                                    <StyledFormCard>
                                        <MissionDefinitionEditDialogContent
                                            missionDefinition={selectedMissions[0]}
                                            fieldName="autoScheduleFrequency"
                                            closeEditDialog={closeDialog}
                                        />
                                    </StyledFormCard>
                                )}
                            </StyledDialog.CustomContent>
                        </StyledDialog>
                    )}
                </StyledSection>
            )}
        </>
    )
}
export const AutoScheduleSection = () => {
    return AutoScheduleList()
}
