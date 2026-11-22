using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Calavera
{
    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        [Header("Configuración de Firebase Firestore")]
        [Tooltip("Reemplaza con el ID real de tu proyecto en Firebase Console")]
        public string projectId = "TU-PROYECTO-ID";
        public string collectionName = "progreso_jugadores";
        public string playerId = "jugador_principal";

        [Header("Datos Sincronizados (Tiempo Real)")]
        public int monedas = 0;
        public bool nivel1Completado = false;
        public bool nivel2Completado = false;

        // Evento para actualizar tus textos UI en pantalla cuando carguen los datos
        public event Action OnDatosCargados;

        private string DocumentUrl =>
            $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/{collectionName}/{playerId}";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Al abrir el juego, descarga el progreso guardado en Firestore
            CargarProgreso();
        }

        // =====================================================================
        // MÉTODOS PÚBLICOS PARA LLAMAR DESDE TU JUEGO
        // =====================================================================

        public void SumarMonedas(int cantidad)
        {
            monedas += cantidad;
            GuardarProgreso();
        }

        public void RegistrarNivelCompletado(int numeroNivel, int monedasGanadas)
        {
            if (numeroNivel == 1) nivel1Completado = true;
            if (numeroNivel == 2) nivel2Completado = true;

            monedas += monedasGanadas;
            GuardarProgreso();
        }

        public void GuardarProgreso()
        {
            StartCoroutine(EnviarDatosFirestore());
        }

        public void CargarProgreso()
        {
            StartCoroutine(ObtenerDatosFirestore());
        }

        // =====================================================================
        // PETICIONES REST A CLOUD FIRESTORE
        // =====================================================================

        private IEnumerator EnviarDatosFirestore()
        {
            // Formato JSON estándar requerido por la API REST de Firestore
            string jsonBody = $@"{{
                ""fields"": {{
                    ""monedas"": {{ ""integerValue"": ""{monedas}"" }},
                    ""nivel1Completado"": {{ ""booleanValue"": {nivel1Completado.ToString().ToLower()} }},
                    ""nivel2Completado"": {{ ""booleanValue"": {nivel2Completado.ToString().ToLower()} }}
                }}
            }}";

            using (UnityWebRequest request = new UnityWebRequest(DocumentUrl, "PATCH"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Firestore] Progreso guardado: {monedas} monedas | N1: {nivel1Completado} | N2: {nivel2Completado}");
                }
                else
                {
                    Debug.LogError($"[Firestore] Error al guardar: {request.error} - {request.downloadHandler.text}");
                }
            }
        }

        private IEnumerator ObtenerDatosFirestore()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(DocumentUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    monedas = ExtraerEnteroFirestore(json, "monedas");
                    nivel1Completado = ExtraerBooleanoFirestore(json, "nivel1Completado");
                    nivel2Completado = ExtraerBooleanoFirestore(json, "nivel2Completado");

                    Debug.Log($"[Firestore] Datos cargados: {monedas} monedas | N1: {nivel1Completado} | N2: {nivel2Completado}");
                    OnDatosCargados?.Invoke();
                }
                else if (request.responseCode == 404)
                {
                    // Si es la primera vez que juega y no existe documento, lo crea inicializado en 0
                    Debug.Log("[Firestore] Jugador nuevo detectado. Creando registro inicial...");
                    GuardarProgreso();
                }
                else
                {
                    Debug.LogError($"[Firestore] Error al cargar: {request.error}");
                }
            }
        }

        // Helpers ligeros para leer la respuesta de Firestore sin librerías externas
        private int ExtraerEnteroFirestore(string json, string campo)
        {
            string clave = $"\"{campo}\":";
            int idx = json.IndexOf(clave, StringComparison.Ordinal);
            if (idx == -1) return 0;

            string marcador = "\"integerValue\": \"";
            int start = json.IndexOf(marcador, idx, StringComparison.Ordinal);
            if (start == -1) return 0;

            start += marcador.Length;
            int end = json.IndexOf("\"", start, StringComparison.Ordinal);
            int.TryParse(json.Substring(start, end - start), out int resultado);
            return resultado;
        }

        private bool ExtraerBooleanoFirestore(string json, string campo)
        {
            string clave = $"\"{campo}\":";
            int idx = json.IndexOf(clave, StringComparison.Ordinal);
            if (idx == -1) return false;

            string marcador = "\"booleanValue\": ";
            int start = json.IndexOf(marcador, idx, StringComparison.Ordinal);
            if (start == -1) return false;

            start += marcador.Length;
            return json.Substring(start).TrimStart().StartsWith("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}