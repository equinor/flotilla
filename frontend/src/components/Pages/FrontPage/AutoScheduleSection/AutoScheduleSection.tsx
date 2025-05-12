import { Button, Icon, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { StyledDialog, StyledTableBody, StyledTableCell, TextAlignedButton } from 'components/Styles/StyledComponents'
import { DaysOfWeek, parseAutoScheduledJobIds } from 'models/AutoScheduleFrequency'
import { config } from 'config'
import styled from 'styled-components'
import { capitalizeFirstLetter } from 'utils/StringFormatting'
import { Icons } from 'utils/icons'
import { useState } from 'react'
import { FormCard } from 'components/Pages/MissionDefinitionPage/MissionDefinitionStyledComponents'
import { MissionDefinitionEditDialogContent } from 'components/Pages/MissionDefinitionPage/MissionDefinitionPage'
import { MissionDefinition } from 'models/MissionDefinition'
import { SelectMissionsComponent } from '../MissionOverview/ScheduleMissionDialog/SelectMissionsToScheduleDialog'
import { BackendAPICaller } from 'api/ApiCaller'
import { Link } from 'react-router-dom'

const StyledSection = styled.div`
    display: flex;
    flex-direction: column;
    max-width: 960px;
    gap: 1rem;
`
const StyledTableRow = styled.div`
    display: grid;
    align-items: center;
    gap: 1rem;
    grid-template-columns: 100px auto 100px;
`
const StyledDayOverview = styled.div`
    display: grid;
    gap: 0px;
`
const StyledMissionButton = styled.div`
    display: flex;
    padding-bottom: 30px;
`
const StyledView = styled.div`
    display: flex;
    align-items: flex-start;
`
const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: end;
    @media (max-width: 600px) {
        align-items: start;
    }
    max-width: 960px;
`

const StyledNextAutoMission = styled.div`
    margin-top: 30px;
`

const StyledTable = styled(Table)`
    width: 960px;
    margin-top: 10px;
    border-top: 1px solid #dcdcdc;

    @media (max-width: 960px) {
        width: 95%;
    }
`

const skipAutoScheduledMission = async (missionId: string, timeOfDay: string) => {
    await BackendAPICaller.skipAutoScheduledMission(missionId, timeOfDay)
}

export const allDays = [
    DaysOfWeek.Monday,
    DaysOfWeek.Tuesday,
    DaysOfWeek.Wednesday,
    DaysOfWeek.Thursday,
    DaysOfWeek.Friday,
    DaysOfWeek.Saturday,
    DaysOfWeek.Sunday,
]

const getDayIndexMondaySunday = (date: Date) => (date.getDay() === 0 ? 6 : date.getDay() - 1)

const AutoScheduleMissionTableRow = ({
    day,
    time,
    mission,
}: {
    day: DaysOfWeek
    time: string
    mission: MissionDefinition
}) => {
    const { TranslateText } = useLanguageContext()

    const currentDayOfTheWeek = allDays[getDayIndexMondaySunday(new Date())]

    return (
        <Table.Row key={mission.id + time}>
            <Table.Cell>
                <StyledTableRow>
                    <Typography>{`${time.substring(0, 5)}`}</Typography>
                    <Typography as={Link} to={`${config.FRONTEND_BASE_ROUTE}/mission-definition/${mission.id}`} link>
                        {mission.name}
                    </Typography>
                    {day === currentDayOfTheWeek &&
                        mission.autoScheduleFrequency &&
                        mission.autoScheduleFrequency.autoScheduledJobs &&
                        parseAutoScheduledJobIds(mission.autoScheduleFrequency.autoScheduledJobs)[time] && (
                            <Button
                                style={{ maxWidth: '100px' }}
                                variant="ghost"
                                onClick={() => skipAutoScheduledMission(mission.id, time)}
                            >
                                {TranslateText('SkipAutoMission')}
                            </Button>
                        )}
                </StyledTableRow>
            </Table.Cell>
        </Table.Row>
    )
}

const AutoScheduleList = () => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [dialogOpen, setDialogOpen] = useState<boolean>(false)
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>([])

    const autoScheduleMissionDefinitions = missionDefinitions.filter((m) => m.autoScheduleFrequency)

    const openDialog = () => {
        setDialogOpen(true)
    }
    const closeDialog = () => {
        setDialogOpen(false)
        setSelectedMissions([])
    }

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
                                <AutoScheduleMissionTableRow
                                    key={time + mission.id}
                                    day={day}
                                    time={time}
                                    mission={mission}
                                />
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

    const EditAutoSchedulingButton = () => (
        <TextAlignedButton onClick={openDialog}>
            <Icon name={Icons.Add} size={24} />
            {TranslateText('Edit auto scheduling')}
        </TextAlignedButton>
    )

    const DisplayScheduledMissions = () => (
        <>
            {autoScheduleMissionDefinitions.length > 0 ? (
                <>
                    <Typography>
                        {TranslateText('These missions will be automatically scheduled at the specified time')}
                    </Typography>
                    <StyledDayOverview>
                        <DayOverview />
                    </StyledDayOverview>
                </>
            ) : (
                <Typography>{TranslateText('There are currently no automatically scheduled missions.')}</Typography>
            )}
        </>
    )

    const UpdateAutoSchedulingDialogContent = () => (
        <>
            <StyledDialog.Header>
                <StyledDialog.Title>
                    <Typography variant="h3">
                        {TranslateText('Edit auto scheduling of mission') + ' ' + selectedMissions[0]?.name}
                    </Typography>
                </StyledDialog.Title>
            </StyledDialog.Header>
            <FormCard>
                <MissionDefinitionEditDialogContent
                    missionDefinition={selectedMissions[0]}
                    fieldName="autoScheduleFrequency"
                    closeEditDialog={closeDialog}
                />
            </FormCard>
        </>
    )

    const SelectMissionDialogContent = () => (
        <>
            <StyledDialog.Header>
                <StyledDialog.Title>
                    <Typography variant="h3">{TranslateText('Select mission for auto scheduling')}</Typography>
                </StyledDialog.Title>
            </StyledDialog.Header>
            <StyledDialog.CustomContent>
                <SelectMissionsComponent
                    missions={missionDefinitions}
                    selectedMissions={selectedMissions}
                    setSelectedMissions={setSelectedMissions}
                    multiple={false}
                />
            </StyledDialog.CustomContent>
            <StyledDialog.Actions>
                <Button onClick={closeDialog} variant="outlined" color="primary">
                    {TranslateText('Cancel')}
                </Button>
            </StyledDialog.Actions>
        </>
    )

    return (
        <StyledView>
            <StyledContent>
                <StyledMissionButton>
                    <EditAutoSchedulingButton />
                </StyledMissionButton>
                <StyledSection>
                    <DisplayScheduledMissions />
                </StyledSection>
                <StyledDialog open={dialogOpen}>
                    {selectedMissions.length === 1 ? (
                        <UpdateAutoSchedulingDialogContent />
                    ) : (
                        <SelectMissionDialogContent />
                    )}
                </StyledDialog>
            </StyledContent>
        </StyledView>
    )
}

const ShowLessOrMoreButton = ({
    showMore,
    setShowMore,
}: {
    showMore: boolean
    setShowMore: (newShowMore: boolean) => void
}) => {
    const { TranslateText } = useLanguageContext()

    if (showMore) {
        return (
            <Button
                variant="ghost"
                onClick={() => {
                    setShowMore(false)
                }}
            >
                <Icon name={Icons.UpChevron} size={16} />
                {TranslateText('Show less')}
            </Button>
        )
    }
    return (
        <Button
            variant="ghost"
            onClick={() => {
                setShowMore(true)
            }}
        >
            <Icon name={Icons.DownChevron} size={16} />
            {TranslateText('Show more')}
        </Button>
    )
}

export const NextAutoScheduleMissionView = () => {
    const { TranslateText } = useLanguageContext()
    const { missionDefinitions } = useMissionDefinitionsContext()
    const [showMore, setShowMore] = useState(false)

    const autoScheduleMissionDefinitions = missionDefinitions.filter((m) => m.autoScheduleFrequency)
    const currentDayOfTheWeek = allDays[getDayIndexMondaySunday(new Date())]

    const missionDefinitionList = autoScheduleMissionDefinitions.filter((m) =>
        m.autoScheduleFrequency!.daysOfWeek.includes(currentDayOfTheWeek)
    )

    const timeMissionPairs = missionDefinitionList
        .filter((m) => m.autoScheduleFrequency?.autoScheduledJobs)
        .flatMap((m) =>
            m
                .autoScheduleFrequency!.timesOfDayCET.filter(
                    (time) => parseAutoScheduledJobIds(m.autoScheduleFrequency!.autoScheduledJobs!)[time]
                )
                .map((time) => ({ time, mission: m }))
        )
        .sort((a, b) => (a.time > b.time ? 1 : -1))

    return (
        <>
            {timeMissionPairs.length > 0 && (
                <StyledNextAutoMission>
                    <Typography variant="h5">{TranslateText('Next auto scheduled mission for today')}</Typography>
                    <StyledTable>
                        <StyledTableBody>
                            {!showMore ? (
                                <AutoScheduleMissionTableRow
                                    day={currentDayOfTheWeek}
                                    time={timeMissionPairs[0].time}
                                    mission={timeMissionPairs[0].mission}
                                />
                            ) : (
                                <>
                                    {timeMissionPairs.map(({ time, mission }) => (
                                        <AutoScheduleMissionTableRow
                                            key={time + mission.id}
                                            day={currentDayOfTheWeek}
                                            time={time}
                                            mission={mission}
                                        />
                                    ))}
                                </>
                            )}
                        </StyledTableBody>
                    </StyledTable>
                    {timeMissionPairs.length > 1 && (
                        <ShowLessOrMoreButton showMore={showMore} setShowMore={setShowMore} />
                    )}
                </StyledNextAutoMission>
            )}
        </>
    )
}

export const AutoScheduleSection = () => {
    return AutoScheduleList()
}
