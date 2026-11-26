/** GET/POST /api/admin/exercise-categories — @ADR-f03-thu-vien-bai-tap-003. */
export type CategoryKind = 'MuscleGroup' | 'Equipment';

export interface Category {
  id: string;
  name: string;
}

/** Theo SortOrder: 11 nhóm cơ + 7 dụng cụ ban đầu (@BR-f03-thu-vien-bai-tap-005), mục coach thêm nằm cuối. */
export interface Categories {
  muscleGroups: Category[];
  equipment: Category[];
}

export interface CategoryCreated {
  id: string;
  kind: CategoryKind;
  name: string;
}

/** GET /api/admin/exercises — sắp theo tên. */
export interface ExerciseListItem {
  id: string;
  name: string;
  muscleGroup: string;
  equipment: string;
  hasVideo: boolean;
  isHidden: boolean;
}

/** GET /api/admin/exercises/{id} (ExerciseDetailDto) — liều có cấu trúc (@ADR-f03-thu-vien-bai-tap-006). */
export interface ExerciseDetail {
  id: string;
  name: string;
  muscleGroupId: string;
  muscleGroup: string;
  equipmentId: string;
  equipment: string;
  videoUrl: string | null;
  hasVideo: boolean;
  sets: number | null;
  repsMin: number | null;
  repsMax: number | null;
  rirMin: number | null;
  rirMax: number | null;
  restSeconds: number | null;
  techniqueNotes: string | null;
  commonMistakes: string | null;
  isHidden: boolean;
  version: string;
}

export interface ExerciseFilter {
  q?: string;
  muscleGroupId?: string | null;
  equipmentId?: string | null;
  /** true = chỉ bài đã ẩn (@BR-f03-thu-vien-bai-tap-016). */
  hidden?: boolean;
}

/** Body POST /api/admin/exercises; PUT thêm `version`. */
export interface SaveExerciseBody {
  name: string;
  muscleGroupId: string;
  equipmentId: string;
  videoUrl: string | null;
  sets: number | null;
  repsMin: number | null;
  repsMax: number | null;
  rirMin: number | null;
  rirMax: number | null;
  restSeconds: number | null;
  techniqueNotes: string | null;
  commonMistakes: string | null;
  version?: string;
}
