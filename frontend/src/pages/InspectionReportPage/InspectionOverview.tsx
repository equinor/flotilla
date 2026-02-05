import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Task, TaskStatus } from 'models/Task'
import {
    StyledImageCard,
    StyledImagesSection,
    StyledInspectionCards,
    StyledInspectionContent,
    StyledInspectionData,
    StyledInspectionOverviewDialogView,
    StyledInspectionOverviewSection,
} from './InspectionStyles'
import { Typography } from '@equinor/eds-core-react'
import { formatDateTime } from 'utils/StringFormatting'
import { SmallInspectionImage } from 'pages/InspectionReportPage/InspectionReportImage'
import { useInspectionId } from './SetInspectionIdHook'

const InspectionOverview = ({ tasks }: { tasks: Task[] }) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionId } = useInspectionId()

    return (
        <StyledImagesSection>
            <StyledInspectionCards>
                {Object.keys(tasks).length > 0 &&
                    tasks.map(
                        (task) =>
                            task.status === TaskStatus.Successful && (
                                <StyledImageCard
                                    key={task.id}
                                    onClick={() => switchSelectedInspectionId(task.inspection.isarInspectionId)}
                                >
                                    <SmallInspectionImage task={task} />
                                    <StyledInspectionData>
                                        {task.tagId && (
                                            <StyledInspectionContent>
                                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                                <Typography variant="body_short">{task.tagId}</Typography>
                                            </StyledInspectionContent>
                                        )}
                                        {task.endTime && (
                                            <StyledInspectionContent>
                                                <Typography variant="caption">
                                                    {TranslateText('Timestamp') + ':'}
                                                </Typography>
                                                <Typography variant="body_short">
                                                    {formatDateTime(task.endTime!, 'dd.MM.yy - HH:mm')}
                                                </Typography>
                                            </StyledInspectionContent>
                                        )}
                                    </StyledInspectionData>
                                </StyledImageCard>
                            )
                    )}
            </StyledInspectionCards>
        </StyledImagesSection>
    )
}

export const InspectionOverviewSection = ({ tasks }: { tasks: Task[] }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledInspectionOverviewSection>
            <Typography variant="h4">{TranslateText('Last completed inspection')}</Typography>
            <InspectionOverview tasks={tasks} />
        </StyledInspectionOverviewSection>
    )
}

export const InspectionOverviewDialogView = ({ tasks }: { tasks: Task[] }) => {
    return (
        <StyledInspectionOverviewDialogView>
            <InspectionOverview tasks={tasks} />
        </StyledInspectionOverviewDialogView>
    )
}
