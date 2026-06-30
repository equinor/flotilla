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
import { SmallAnalysisResult, SmallInspectionResult } from 'pages/InspectionReportPage/InspectionReportImage'
import { useInspectionId } from './SetInspectionIdHook'

type OverviewVariant = 'inspection' | 'analysis'

const ImageOverview = ({ tasks, variant }: { tasks: Task[]; variant: OverviewVariant }) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionId, switchSelectedAnalysisId } = useInspectionId()

    const onSelect = variant === 'analysis' ? switchSelectedAnalysisId : switchSelectedInspectionId
    const renderImage = (task: Task) =>
        variant === 'analysis' ? <SmallAnalysisResult task={task} /> : <SmallInspectionResult task={task} />

    return (
        <StyledImagesSection>
            <StyledInspectionCards>
                {tasks.map(
                    (task) =>
                        task.status === TaskStatus.Successful && (
                            <StyledImageCard key={task.id} onClick={() => onSelect(task.inspection.isarInspectionId)}>
                                {renderImage(task)}
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
                                                {formatDateTime(task.endTime!)}
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

const OverviewSection = ({ tasks, variant, title }: { tasks: Task[]; variant: OverviewVariant; title: string }) => {
    if (!tasks.some((task) => task.status === TaskStatus.Successful)) return <></>
    return (
        <StyledInspectionOverviewSection>
            <Typography variant="h4">{title}</Typography>
            <ImageOverview tasks={tasks} variant={variant} />
        </StyledInspectionOverviewSection>
    )
}

const OverviewDialogView = ({ tasks, variant }: { tasks: Task[]; variant: OverviewVariant }) => (
    <StyledInspectionOverviewDialogView>
        <ImageOverview tasks={tasks} variant={variant} />
    </StyledInspectionOverviewDialogView>
)

export const InspectionOverviewSection = ({ tasks }: { tasks: Task[] }) => {
    const { TranslateText } = useLanguageContext()
    return <OverviewSection tasks={tasks} variant="inspection" title={TranslateText('Inspection result')} />
}

export const AnalysisOverviewSection = ({ tasks }: { tasks: Task[] }) => {
    const { TranslateText } = useLanguageContext()
    return <OverviewSection tasks={tasks} variant="analysis" title={TranslateText('Analysis result')} />
}

export const InspectionOverviewDialogView = ({ tasks }: { tasks: Task[] }) => (
    <OverviewDialogView tasks={tasks} variant="inspection" />
)

export const AnalysisOverviewDialogView = ({ tasks }: { tasks: Task[] }) => (
    <OverviewDialogView tasks={tasks} variant="analysis" />
)
